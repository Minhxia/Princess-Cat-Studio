using UnityEngine;
using UnityEngine.AI;
using BehaviourAPI.Core;

// Acción del estado Matar. Se encarga de detener al monstruo y comunica la derrota.
public class MonsterKillAction
{
    private readonly NavMeshAgent _agent;

    public MonsterKillAction(NavMeshAgent agent)
    {
        _agent = agent;
    }

    // Comunica el final de partida por derrota
    public void Begin()
    {
        if (_agent.isOnNavMesh)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
        }

        if (GameManager.Instance != null)
            GameManager.Instance.Lose();
        else
            Debug.LogError("[MonsterKillAction] No hay un GameManager en la escena.");
    }

    // Permanece activo el estado hasta que cambia la escena al GameOver
    public Status Tick()
    {
        return Status.Running;
    }
}
