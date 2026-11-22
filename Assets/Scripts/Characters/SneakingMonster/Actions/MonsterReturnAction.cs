using UnityEngine;
using UnityEngine.AI;
using BehaviourAPI.Core;

// Acción del estado Reiniciar. Se encarga de llevar al monstruo de vuelta a la posición inicial.
public class MonsterReturnAction
{
    private readonly NavMeshAgent _agent;
    private readonly Vector3 _homePosition;
    private readonly float _speed;
    private readonly float _homeTolerance;

    public MonsterReturnAction(NavMeshAgent agent, Vector3 homePosition, float speed,
                               float homeTolerance)
    {
        _agent = agent;
        _homePosition = homePosition;
        _speed = speed;
        _homeTolerance = homeTolerance;
    }

    // Inicia el recorrido de vuelta
    public void Begin()
    {
        if (!_agent.isOnNavMesh)
        {
            Debug.LogError("[MonsterReturnAction] No puede volver, el agente no está en el NavMesh.");
            return;
        }

        // se asginan los valores
        _agent.speed = _speed;
        _agent.isStopped = false;
        if (!_agent.SetDestination(_homePosition))
            Debug.LogWarning("[MonsterReturnAction] No se ha podido calcular el regreso a la posición inicial.");
    }

    // Indica que ha llegado a la posición inicial
    public Status Tick()
    {
        if (!_agent.isOnNavMesh || _agent.pathPending)
            return Status.Running;

        // se comprueba su posición actual
        Vector3 difference = _agent.transform.position - _homePosition;
        difference.y = 0f;
        float arrivalDistance = Mathf.Max(_agent.stoppingDistance, _homeTolerance);

        // si la diferencia entre su posición actual y la inicial es lo suficientemente pequeña,
        // ha llegado
        if (difference.sqrMagnitude <= arrivalDistance * arrivalDistance
            && _agent.remainingDistance <= arrivalDistance
            && (!_agent.hasPath || _agent.velocity.sqrMagnitude < 0.01f))
            return Status.Success;

        return Status.Running;
    }
}
