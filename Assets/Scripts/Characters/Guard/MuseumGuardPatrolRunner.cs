using System;
using BehaviourAPI.Core;
using BehaviourAPI.Core.Actions;
using BehaviourAPI.StateMachines;
using BehaviourAPI.UnityToolkit;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]

// Clase que gestiona la máquina de estado del Guardia (las acciones están dentro de los estados)
public class MuseumGuardPatrolRunner : BehaviourRunner
{
    #region Variables
    [Serializable]
    private struct PatrolPoint
    {
        public Transform target;            // punto de destino
        [Min(0f)] public float waitSeconds; // tiempo que se queda esperando en ese punto (si es 0s es un punto de paso)
    }

    [SerializeField] private PatrolPoint[] route = Array.Empty<PatrolPoint>(); // array con las rutas del guardia

    private NavMeshAgent _agent;
    private int _pointIndex;         // en qué punto de la ruta está (route[_pointIndex])
    private float _waitRemaining;    // tiempo de espera
    private Status _moveStartStatus; // resultado de empezar a moverse
    #endregion

    private PatrolPoint CurrentPoint => route[_pointIndex];

    #region FSM
    // Conecta los métodos con BehaviourAPI
    protected override BehaviourGraph CreateGraph()
    {
        _agent = GetComponent<NavMeshAgent>();

        FSM fsm = new FSM();

        // ESTADOS:
        // IR AL PUNTO: al entrar en el estado, prepara el movimiento (BeginMove) y mientras
        // está activo comprueba si ha llegado (CheckArrival)
        State move = fsm.CreateState("MoveToPoint", new FunctionalAction(BeginMove, CheckArrival));
        // PARADA: al entrar en el estado, prepara la pausa (BeginWait) y mientras está activo
        // comprueba cuánto tiempo queda (UpdateWait)
        State wait = fsm.CreateState("WaitAtPoint", new FunctionalAction(BeginWait, UpdateWait));

        // TRANSICIONES:
        // conecta ir al punto -> parada
        fsm.CreateTransition("Arrived", move, wait,
            statusFlags: StatusFlags.Success); // la transición se activa cuando CheckArriva devuelve Sucess
        // conecta parada -> ir al punto
        fsm.CreateTransition("NextPoint", wait, move,
            action: new FunctionalAction(AdvancePoint), // se calcula el siguiente destino
            statusFlags: StatusFlags.Success);  // la transición se activa cuando UpdateWait devuelve Sucess

        // se establece el primer estado
        fsm.SetEntryState(move);
        // se envía la máquina de estados
        return fsm;
    }
    #endregion

    #region Methods
    // Prepara el inicio del movimiento
    private void BeginMove()
    {
        if (route.Length == 0 || CurrentPoint.target == null)
        {
            _moveStartStatus = FailMove("Faltan por asignar el destino en Route (Inspector).");
            return;
        }

        if (!_agent.isOnNavMesh)
        {
            _moveStartStatus = FailMove("El guardia está fuera del NavMesh.");
            return;
        }

        // se lee el siguiente destino
        Vector3 destination = CurrentPoint.target.position;

        // si está lo suficientemente cerca del destino (a una distancia muy pequeña),
        // se da el tramo por completado
        if (Vector3.Distance(_agent.nextPosition, destination) <= 0.05f)
        {
            _moveStartStatus = Status.Success;
            return;
        }

        // se quita la pausa, porque ha comenzado a moverse
        _agent.isStopped = false;
        // en caso de que el punto de destino tenga un tiempo de espera, el _agente empieza
        // a frenarse automáticamente al acercarse a dicho punto
        _agent.autoBraking = CurrentPoint.waitSeconds > 0f;

        _moveStartStatus = _agent.SetDestination(destination)
            ? Status.Running    // en proceso, hay ruta
            : FailMove("Error con el destino. No hay ruta hasta ese punto.");
    }

    // Comprueba el progreso hacia el punto de destino
    private Status CheckArrival()
    {
        // si al comenzar el movimiento, el guardia ya está en el punto de destino o
        // se produce un fallo, devuelve dicho resultado
        if (_moveStartStatus != Status.Running)
            return _moveStartStatus;

        // si durante el movimiento, el guardia se sale del NavMesh
        if (!_agent.isOnNavMesh)
            return FailMove("El guardia se ha salido del NavMesh.");

        // si aún no se ha calculado la ruta
        if (_agent.pathPending)
            return Status.Running;

        // se calcula la distancia a partir de la cual se considera que ha llegado
        // al punto de destno
        float arrivalDistance = _agent.stoppingDistance + 0.05f;

        // en caso de que no tenga ruta
        if (!_agent.hasPath)
        {
            // se calcula la distancia restante hasta el punto de destino
            float distance = Vector3.Distance(
                _agent.nextPosition, CurrentPoint.target.position);

            // se comprueba si no tiene porque ha llegado o por un fallo
            return distance <= arrivalDistance
                ? Status.Success
                : FailMove("El guardia se ha quedado sin ruta antes de llegar.");
        }

        // en cambio, si tiene ruta, se comprueba si está completa (es decir, que llegue
        // al punto de destino que corresponda, por ejemplo, una sala del museo)
        if (_agent.pathStatus != NavMeshPathStatus.PathComplete)
            return FailMove($"La ruta no está completa ({_agent.pathStatus}).");

        // finalmente, se mide la distancia que le queda por recorrer de la ruta
        return _agent.remainingDistance <= arrivalDistance
            ? Status.Success
            : Status.Running;
    }

    // Comienza la espera en el punto de destino
    private void BeginWait()
    {
        // tiempo restante de la espera
        _waitRemaining = CurrentPoint.waitSeconds;

        if (_waitRemaining > 0f)
            _agent.isStopped = true;
    }

    // Actuliza el tiempo restante de la espera y devuelve el resultado
    private Status UpdateWait()
    {
        _waitRemaining -= Time.deltaTime;
        return _waitRemaining <= 0f ? Status.Success : Status.Running;
    }

    // Calcula el siguiente punto de destino (en caso de estar en el último punto,
    // empieza desde el principio otra vez)
    private void AdvancePoint()
    {
        _pointIndex = (_pointIndex + 1) % route.Length;
    }

    // Indica que se ha producido un fallo
    private Status FailMove(string reason)
    {
        // se detiene al guardia
        if (_agent.isOnNavMesh)
            _agent.isStopped = true;

        Debug.LogError("[MuseumGuardPatrolRunner] Route[" + _pointIndex + "]. " + reason, this);
        return Status.Failure;
    }
    #endregion
}
