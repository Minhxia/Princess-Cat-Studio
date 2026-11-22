using UnityEngine.AI;
using BehaviourAPI.Core;

// Acción del estado Esperar. Se encarga de mantener al monstruo en su posición inicial
// hasta que el jugador entra en su zona.
public class MonsterWaitAction
{
    private readonly NavMeshAgent _agent;

    public MonsterWaitAction(NavMeshAgent agent)
    {
        _agent = agent;
    }

    // Detiene al monstruo
    public void Begin()
    {
        if (!_agent.isOnNavMesh)
            return;

        _agent.isStopped = true;
        _agent.ResetPath();
    }

    // Permanece en el estado (sale por las percepciones)
    public Status Tick()
    {
        return Status.Running;
    }
}
