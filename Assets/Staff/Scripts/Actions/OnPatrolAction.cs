using UnityEngine;
using UnityEngine.AI;
using BehaviourAPI.Core;
using BehaviourAPI.UnityToolkit;

public class OnPatrolAction : UnityAction
{
    private Transform[] _points;
    private NavMeshAgent _agent;
    private int _nextPoint;

    public OnPatrolAction() { }

    public OnPatrolAction(NavMeshAgent agent, Transform[] points) 
    {
        _agent = agent;
        _points = points;
        // se inicializa la ruta en la primera posición
        _nextPoint = 0;
    }

    public override void Start()  
    {
        _agent.autoBraking = false;  // al desactivarlo, se permite un movimiento continuo entre los puntos
        GotoNextPoint();        
    }

    public override Status Update() 
    {
        //Debug.Log("[OnPatrol] agent.pathPending: " + agent.pathPending);
        //Debug.Log("[OnPatrol] agent.remainingDistance: " + agent.remainingDistance);

        if (!_agent.pathPending && _agent.remainingDistance < 0.5f) 
        {
            Debug.Log("[OnPatrol] Returned Success!");
            return Status.Success;
        }
        
        return Status.Running;
    }

    public override void Stop() 
    {
        Debug.Log("[OnPatrol] Stopped");

        // se resetea el estado del agente
        // this.agent.destination = this.agent.gameObject.transform.position;
        // this.agent.ResetPath();
    }

    // Asigna el siguiente punto de destino del agente
    void GotoNextPoint() 
    {
        if (_points.Length == 0) 
        {
            Debug.LogWarning("[OnPatrol] No se han establecido los puntos para patrullar");
            return;
        }

        // se le asigna el punto de destino al agente y se actualiza el índice para el siguiente
        _agent.destination = _points[_nextPoint].position;
        // se crea un bucle para que el índice se reinicie cuando alcance el final del array
        _nextPoint = (_nextPoint + 1) % _points.Length;
    }
}
