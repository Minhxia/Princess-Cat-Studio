using UnityEngine.AI;
using BehaviourAPI.Core;

// Acción del estado Parar. Se encarga de mantener al monstruo inmóvil durante todo el tiempo
// que el jugador lo esté observando.
public class MonsterStopAction
{
    private readonly NavMeshAgent _agent;

    public MonsterStopAction(NavMeshAgent agent)
    {
        _agent = agent;
    }

    // Detiene el agente
    public void Begin()
    {
        if (!_agent.isOnNavMesh)
            return;

        _agent.isStopped = true;
        _agent.ResetPath();
    }

    // Permanece en el estado (sale por las transiciones)
    public Status Tick()
    {
        return Status.Running;
    }
}
