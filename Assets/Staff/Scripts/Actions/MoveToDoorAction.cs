using UnityEngine;
using UnityEngine.AI;
using BehaviourAPI.Core;
using BehaviourAPI.UnityToolkit;

public class MoveToDoorAction : UnityAction
{
    private Transform[] _points;
    private NavMeshAgent _agent;

    public MoveToDoorAction() { }

    public MoveToDoorAction(NavMeshAgent agent, Transform[] points) 
    {
        _agent = agent;
        _points = points;
    }

    public override void Start() 
    {
        _agent.autoBraking = false;
        // TODO habría que cambiarlo para que no vaya siempre a la izquierda, que se coordine con los otros guardias
        _agent.destination = _points[0].position;
    }

    public override Status Update() 
    {
        Debug.Log("[MoveToDoor] agent.pathPending: " + _agent.pathPending);
        Debug.Log("[MoveToDoor] agent.remainingDistancce: " + _agent.remainingDistance);
	
        if (!_agent.pathPending && _agent.remainingDistance < 0.5f) 
        {
            Debug.Log("[MoveToDoor] Returned Success");
            return Status.Success;
	    }

        return Status.Running;
    }

    public override void Stop() 
    {
        Debug.Log("[MoveToBar] Stopped");
        //agent.destination = agent.gameObject.transform.position;
        //agent.ResetPath();
    }
}
