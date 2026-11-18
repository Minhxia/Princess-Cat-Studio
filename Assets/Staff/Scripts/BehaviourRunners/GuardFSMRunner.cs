using UnityEngine;
using UnityEngine.AI;
using BehaviourAPI.Core;
using BehaviourAPI.Core.Perceptions;
using BehaviourAPI.StateMachines;
using BehaviourAPI.UnityToolkit;
using BehaviourAPI.UnityToolkit.GUIDesigner.Runtime;

public class GuardFSMRunner : BehaviourRunner
{
    [SerializeField] private Transform[] _doorPositions;
    [SerializeField] private Transform[] _watchZones; 
    
    private NavMeshAgent _agent;
    private BSRuntimeDebugger _debugger;
    private PushPerception _goToDoor;
    private bool _mustGoToDoor;

    protected override void Init() 
    {
        _agent = GetComponent<NavMeshAgent>();
        _debugger = GetComponent<BSRuntimeDebugger>();
        _mustGoToDoor = false;
        base.Init();
    }
    
    protected override BehaviourGraph CreateGraph() 
    {
        var guardFSM = new FSM();

        // Percepciones
        //var watchTimer = new TimerPerception(3);    // tiempo observando
        //var guardTimer = new TimerPerception(5);    // tiempo vigilando la puerta
        var watchTimer = new UnityTimePerception(3f);
        var guardTimer = new UnityTimePerception(5f);

        // Estados
        // Estado Puerta -> Subestados: MoverseALaPuerta y Observar
        //State movingToDoorState = guardFSM.CreateState("MovingToDoor", new MoveToDoorAction(agent, doorPositions));
        State guardDoorState = guardFSM.CreateState("GuardDoor", new DebugLogAction("[Guard] Guarding the door"));

        // Estado Patrulla -> Subestados: Observar y Patrullar
        State watchState = guardFSM.CreateState("Watch", new DebugLogAction("[Guard] Watching the zone"));      
        State patrolState = guardFSM.CreateState("Patrol", new OnPatrolAction(_agent, _watchZones));
        State movingToDoorState = guardFSM.CreateState("MovingToDoor", new MoveToDoorAction(_agent, _doorPositions));

        // Transiciones
        guardFSM.CreateTransition("watch to moving", watchState, patrolState, watchTimer);
        guardFSM.CreateTransition("moving to watch", patrolState, watchState, statusFlags: StatusFlags.Finished);
        
        // Activación del cambio de patrullar a vigilar la puerta
        // mediante percecpción tipo push
        StateTransition patrolToMoveToDoor = guardFSM.CreateTransition("moving to move to the door", patrolState, movingToDoorState, statusFlags: StatusFlags.None);
        StateTransition watchToMoveToDoor = guardFSM.CreateTransition("watch to move to the door", watchState, movingToDoorState, statusFlags: StatusFlags.None);
        _goToDoor = new PushPerception(patrolToMoveToDoor, watchToMoveToDoor);
        
        guardFSM.CreateTransition("move to door to guard the door", movingToDoorState, guardDoorState, statusFlags: StatusFlags.Finished);
        guardFSM.CreateTransition("guard the door to moving", guardDoorState, patrolState, guardTimer, new ShiftChangeAction());
        
        // se indica cuál es el estado inicial, como todos los guardias empiezan dentro el estado inicial
        // será moverse
        guardFSM.SetEntryState(patrolState);
        
        _debugger.RegisterGraph(guardFSM);

        return guardFSM;
    }

    public void ReceiveOrderFromManager() 
    {
        _mustGoToDoor = true;
    }

    protected override void OnUpdated()
    {
        if(_mustGoToDoor) 
        {
            _goToDoor.Fire();
            _mustGoToDoor = false;            
        }
        base.OnUpdated();
    }
}
