using UnityEngine;
using UnityEngine.AI;
using BehaviourAPI.Core;

// Acción del estado Perseguir. Se encarga de actualizar el destino del NavMeshAgent
// mientras el jugador esté en la zona y no esté viendo al monstruo.
public class MonsterChaseAction
{
    private readonly NavMeshAgent _agent;
    private readonly Transform _player;
    private readonly float _speed;
    private readonly float _repathSeconds;

    private float _nextRepathTime;

    public MonsterChaseAction(NavMeshAgent agent, Transform player, float speed, float repathSeconds)
    {
        _agent = agent;
        _player = player;
        _speed = speed;
        _repathSeconds = repathSeconds;
    }

    // Prepara la ruta hacia el jugador
    public void Begin()
    {
        if (!_agent.isOnNavMesh)
        {
            Debug.LogError("[MonsterChaseAction] No puede perseguir, el agente no está en el NavMesh.");
            return;
        }

        // reseteo de los valores
        _agent.speed = _speed;
        _agent.isStopped = false;
        _nextRepathTime = 0f;
    }

    // Actualiza el destino cada cierto tiempo para seguir la posición del jugador
    public Status Tick()
    {
        if (_player == null || !_agent.isOnNavMesh)
            return Status.Running;

        // cuando ha transcurrido un tiempo, vuelve a buscar la posición
        if (Time.time >= _nextRepathTime)
        {
            _nextRepathTime = Time.time + _repathSeconds;
            _agent.SetDestination(_player.position);
        }

        return Status.Running;
    }
}
