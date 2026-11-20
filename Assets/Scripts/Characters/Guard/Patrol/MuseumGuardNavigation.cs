using UnityEngine;
using BehaviourAPI.Core;
using UnityEngine.AI;

// Clase que se encarga de mover al guardia hasta su destino.
public class MuseumGuardNavigation
{
    #region Variables
    private const float ArrivalTolerance = 0.05f;   // para detectar si ha alcanzado el destino

    private readonly NavMeshAgent _agent;
    private readonly MuseumGuardPatrolRoute _route;
    private readonly float _sampleRadius;           // para calcular un punto cerca del destino
    private readonly float _retrySeconds;           // tiempo para volver a intentar encontrar una ruta
    private readonly bool _agentControlsRotation;   // si el agente gira solo
    private readonly float _patrolStoppingDistance; // distancia a la que para

    private Vector3 _destination;       // punto de destino calculado
    private Status _travelStatus;       // en qué estado se encuentra la ruta: éxito, en proceso o fallo
    private float _retryAt;             // punto en el que reintentar encontrar una ruta
    private bool _pathWarningIssued;    // indica si se ha mostrado ya el feedback
    private bool _wasMoving;            // indica si el guardia se estaba moviendo (antes de parar)
    private Transform _travelTarget;    // punto al que se dirige el guardia (punto de patrulla, cafetera, panel, silla, etc.)
    private bool _usePatrolSettings;    // indica si el desplazamiento es el de la patrulla o el de descanso

    private bool AgentOnNavMesh => _agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh;
    public bool IsWalking => AgentOnNavMesh && !_agent.isStopped && _agent.velocity.sqrMagnitude > 0.01f;
    #endregion

    public MuseumGuardNavigation(NavMeshAgent agent, MuseumGuardPatrolRoute route, float sampleRadius,
        float retrySeconds)
    {
        _agent = agent;
        _route = route;
        _sampleRadius = sampleRadius;
        _retrySeconds = retrySeconds;
        _agentControlsRotation = agent.updateRotation;
        _patrolStoppingDistance = agent.stoppingDistance;
    }

    #region Movement
    // Inicia el desplazamiento en un tramo (ej: A -> B) o a un punto concreto (ej: cafetera)
    // (por defecto se usan los puntos de patrulla -> usePatrolSettings = true)
    public void BeginTravel(Transform target, bool usePatrolSettings = true)
    {
        _travelTarget = target;
        _usePatrolSettings = usePatrolSettings;
        _pathWarningIssued = false;
        RequestPath();
    }

    // Interrumpe el desplazamiento del guardia (porque ha visto al jugador)
    public void Suspend()
    {
        _wasMoving = AgentOnNavMesh && !_agent.isStopped;
        Stop();
    }

    // Para al guardia como tal
    public void Stop()
    {
        if (AgentOnNavMesh)
            _agent.isStopped = true;
    }

    // Reanuda el desplazamiento
    public void Resume()
    {
        if (_wasMoving && AgentOnNavMesh)
            _agent.isStopped = false;
    }

    // Permite girar manualmente (desde código) al guardia
    public void SetManualRotation()
    {
        _agent.updateRotation = false;
    }
    #endregion

    #region Path
    // Prepara al guardia (agente) y su siguiente destino
    private void RequestPath()
    {
        // primero se decide si se activa el frenado automático en función del tipo de waypoint
        // y de si tiene inspección
        bool autoBrake = !_usePatrolSettings || !_route.AtPatrolPoint
                                || _route.CurrentWaitSeconds > 0f;
        _travelStatus = Status.Running;

        // se comprueba si hay un punto al que ir
        if (_travelTarget == null)
        {
            RetryTravel("Falta el punto de destino.");
            return;
        }

        // se comprueba que el guardia esté en el NavMesh
        if (!AgentOnNavMesh)
        {
            RetryTravel("El guardia está fuera del NavMesh.");
            return;
        }

        // se busca un punto al que el NavMesh pueda ir que esté lo más cerca del waypoint
        NavMeshQueryFilter filter = new NavMeshQueryFilter { agentTypeID = _agent.agentTypeID, areaMask = _agent.areaMask };
        if (!NavMesh.SamplePosition(_travelTarget.position, out NavMeshHit hit, _sampleRadius, filter))
        {
            RetryTravel("El punto no tiene NavMesh cerca.");
            return;
        }

        // se configura el guardia (su agente para el NavMesh)
        _destination = hit.position;
        _agent.updateRotation = _agentControlsRotation;
        _agent.stoppingDistance = _usePatrolSettings && _route.AtPatrolPoint ? _patrolStoppingDistance : 0f;
        _agent.autoBraking = autoBrake;
        // se comprueba si ha llegado a su destino, mirando si está lo suficientemente cerca
        if (Vector3.Distance(_agent.nextPosition, _destination) <= ArrivalTolerance)
        {
            // si ha llegado, se marca ese tramo como completado
            _travelStatus = Status.Success;
            // y se para si es necesario
            if (autoBrake)
                Stop();
            return;
        }

        // y si no, se sigue hacia ese destino
        if (_agent.SetDestination(_destination))
            _agent.isStopped = false;
        else
            RetryTravel("El guardia no puede ir al destino.");
    }

    // Prepara otro intento para que el guardia alcance su destino
    private Status RetryTravel(string message)
    {
        // se detiene al guardia
        Stop();
        _travelStatus = Status.Failure;
        // se calcula cuándo realizar otro intento para llegar al destino
        _retryAt = Time.time + _retrySeconds;
        // y se muestra el feedback
        if (!_pathWarningIssued)
        {
            _pathWarningIssued = true;
            // para evitar buscar el nombre de un gameobjet nulo
            string targetName = "(sin asignar)";
            if (_travelTarget != null)
                targetName = _travelTarget.name;
            Debug.LogWarning("[MuseumGuardPatrolRoute]" + message + " Se reintentará el tramo: sala = "
                            + _route.CurrentRoomID + ", punto = " + targetName);
        }
        return Status.Running;
    }

    // Indica si el guardia ya ha llegado a su destino
    public Status CheckArrival()
    {
        // si ha llegado bien, sale
        if (_travelStatus == Status.Success)
            return Status.Success;
        // si ha tenido un problema al llegar al destino, se recalcula la ruta
        if (_travelStatus == Status.Failure)
        {
            if (Time.time >= _retryAt)
                RequestPath();
            return Status.Running;
        }
        // si se sale del NavMesh, también se intenta recalcular la ruta
        if (!AgentOnNavMesh)
            return RetryTravel("El agente se ha salido del NavMesh.");
        // y si aún no ha llegado, sigue
        if (_agent.pathPending)
            return Status.Running;

        // se comprueba la distancia a la que se considera que ha llegado a su destino
        float arrivalDistance = _agent.stoppingDistance + ArrivalTolerance;
        // primero, se mira si no tiene ruta
        if (!_agent.hasPath)
        {
            // en ese caso, se vuelve a comprobar la distancia entre su punto y el destino,
            // y en función de eso sale o vuelve a recalcular la ruta
            return Vector3.Distance(_agent.nextPosition, _destination) <= arrivalDistance
                        ? Status.Success : RetryTravel("Se ha perdido la ruta antes de llegar.");
        }

        // en cambio, si tiene ruta pero no está completa (no llega hasta el destino),
        // también se recalcula
        if (_agent.pathStatus != NavMeshPathStatus.PathComplete)
            return RetryTravel("No hay una ruta completa hasta el destino.");

        // finalmente, se comprueba si ha llegado o no para devolver el resultado
        return _agent.remainingDistance <= arrivalDistance ? Status.Success : Status.Running;
    }
    #endregion
}
