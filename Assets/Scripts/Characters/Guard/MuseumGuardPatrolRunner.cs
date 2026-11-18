using System;
using BehaviourAPI.Core;
using BehaviourAPI.Core.Actions;
using BehaviourAPI.Core.Perceptions;
using BehaviourAPI.StateMachines;
using BehaviourAPI.UnityToolkit;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

[RequireComponent(typeof(NavMeshAgent))]

// Clase que monta y gestiona las máquinas de estado del Guardia (las acciones están dentro de los estados)
public class MuseumGuardPatrolRunner : BehaviourRunner
{
    #region Variables
    // Waypoint
    [Serializable]
    public struct PatrolPoint
    {
        public Transform target;            // posición
        [Min(0f)] public float waitSeconds; // tiempo que dura la inspección con la linterna
    }

    // Conexión con una sala hija
    [Serializable]
    public class Door
    {
        [Min(-1)] public int childRoomID = -1;  // -1 = rama vacía
        [Min(-1)] public int afterPoint = -1;   // indica después de qué waypoint se cruza de una sala a otra

        // puntos a cada lado de la puerta
        public Transform parentSide; // por ejemplo: recepción es la sala padre
        public Transform childSide;  // tienda regalos es la sala hija
    }

    // Sala con sus datos
    [Serializable]
    public class Room
    {
        public int id = MuseumGuardPatrolRoute.HomeRoomID;       // HomeRoomID = sala inicial
        public PatrolPoint[] points = Array.Empty<PatrolPoint>(); // array con los waypoints de la ruta en la sala (se añaden a mano)
        // conexiones con otras salas
        public Door leftDoor = new Door();
        public Door rightDoor = new Door();
    }

    // Evento que se lanza cuando el guardia ve al jugador, que contiene el ID de la sala
    // y la posición del jugador
    [Serializable] public class GuardSightingEvent : UnityEvent<int, Vector3> { }

    [Header("Patrol")]
    [SerializeField] private Room[] _rooms = Array.Empty<Room>();   // recorrido por las salas
    [SerializeField] private Transform _restPoint;  // punto al que vuelve el guardia a descansar (sala de seguridad)
    [SerializeField, Min(0f)] private float _restSeconds = 60f; // tiempo que está en ese estado de Pausa (su descanso tras la patrulla)
    [SerializeField] private bool _restOnStart;     // indica si empieza la patrulla en Pausa
    // para recalcular la ruta del navmesh:
    [SerializeField, Min(0.01f)] private float _navMeshSampleRadius = 0.35f;
    [SerializeField, Min(0.1f)] private float _pathRetrySeconds = 2f;

    [Header("Inspection")]
    [SerializeField] private Transform _player;             // para obtener la posición del jugador
    [SerializeField] private Transform _playerVisionTarget; // objeto del jugador que el guardia ve (por ejemplo, su body)
    [SerializeField] private Transform _visionOrigin;               // desde dónde mira el guardia
    [SerializeField, Min(0.1f)] private float _visionRange = 10f;   // hasta dónde ve el guardia
    [SerializeField, Range(1f, 180f)] private float _visionAngle = 55f; // cono de visión
    [SerializeField, Range(0f, 180f)] private float _inspectionpAngle = 90f;  // giro del guardia al inspeccionar
    [SerializeField] private LayerMask _sightObstacles = Physics.DefaultRaycastLayers; // objetos que no dejan ver

    [Header("Sighting")]
    [SerializeField] private GuardSightingEvent _onPlayerSpotted = new GuardSightingEvent();

    [Header("Animation")]
    [SerializeField] private Animator _animator;

    private State _inspectionState;     // para las animaciones

    // Referencias
    private MuseumGuardPatrolRoute _patrol;     // guarda el recorrido
    private MuseumGuardNavigation _navigation;  // desplaza al guardia hasta el siguiente waypoint
    private MuseumGuardVision _vision;          // qué ve el guardia
    private SendAlertAction _sendAlert;         // acción que envía el aviso al monstruo

    // FSM
    private FSM _movement;              // FSM Nivel 2: Movimiento
    private State _activeMovementState; // estado activo dentro de la FSM Nivel 2
    private PushPerception _enterRest;  // señal para pasar al estado Pausa desde RetrocederSalaInicial
    private PushPerception _leaveRest;  // señal para pasar al estado Andar desde Pausa
    private PushPerception _pendingRestSignal; // señal de las anteriores que queda por lanzar
    private bool _resumeMovement; // después de haber visto al jugador, reanuda la acción donde la había dejado
    #endregion

    #region Getters
    // Devuelve el ID de la sala actual
    public int CurrentRoomID
    {
        get
        {
            if (_patrol == null)
            {
                return MuseumGuardPatrolRoute.HomeRoomID;   // sala inicial
            }

            return _patrol.CurrentRoomID;
        }
    }

    // Devuelve la referencia al evento en que el guardia ve al jugador
    public GuardSightingEvent OnPlayerSpotted => _onPlayerSpotted;
    #endregion

    #region Unity
    private void LateUpdate()
    {
        // si la patrulla está activa (no se ha interrumpido) y hay un
        // señal pendiente (es decir: _enterRest o _leaveRest)
        if (!_resumeMovement && _pendingRestSignal != null)
        {
            PushPerception signal = _pendingRestSignal;
            _pendingRestSignal = null;

            if (signal == _leaveRest)
                Debug.Log("[Guardia] Disparando DescansoTerminado", this);

            // se realiza el cambio de estado
            signal.Fire();
        }

        // se actualizan las animaciones
        UpdateAnimations();
    }
    #endregion

    #region FSMs
    // Crea la máquina de estados de dos niveles
    protected override BehaviourGraph CreateGraph()
    {
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        // se crea la ruta
        _patrol = new MuseumGuardPatrolRoute(_rooms, _restPoint);
        // su navegación
        _navigation = new MuseumGuardNavigation(agent, _patrol, _navMeshSampleRadius, _pathRetrySeconds);
        // su "visión" (es decir, para saber si ve al jugador)
        _vision = new MuseumGuardVision(transform, _player, _playerVisionTarget, _visionOrigin,
                                        _visionRange, _visionAngle, _sightObstacles);
        // y la alerta al ver al jugador (el evento)
        _sendAlert = new SendAlertAction(_vision, _onPlayerSpotted);

        // creación de la FSM con dos niveles
        _movement = CreateMovementFSM(agent);  // FSM Nivel 2
        FSM guard = new FSM();                 // FSM Nivel 1

        // ESTADOS FSM NIVEL 1:
        State movement = guard.CreateState("Movimiento", new SubsystemAction(_movement));
        State sendMonster = guard.CreateState("MandarMonstruo", new FunctionalAction(BeginAlert, UpdateAlert, null));

        // PERCEPCIONES FSM NIVEL 1:
        ConditionPerception playerDetected = new ConditionPerception(null, CheckPlayerDetected, null);

        // TRANSICIONES FSM NIVEL 1:
        // Movimiento -> MandarMonstruo
        StateTransition movToMonster = guard.CreateTransition("DetectarJugador",
                                                movement, sendMonster, playerDetected);
        // MandarMonstruo -> Movimiento
        StateTransition monsterToMov = guard.CreateTransition("AvisoEnviado",
                                sendMonster, movement, statusFlags: StatusFlags.Success);

        // se establece el estado inicial
        guard.SetEntryState(movement);

        return guard;
    }

    // Crea la máquina de estados del nivel 2 (Movimiento)
    private FSM CreateMovementFSM(NavMeshAgent agent)
    {
        // se crea la inspección (que el guardia mire de un lado a otro),
        MuseumGuardInspection inspection = new MuseumGuardInspection(agent, _inspectionpAngle);

        // ACCIONES FSM NIVEL 2:
        RestAction restAction = new RestAction(_patrol, _navigation, _restSeconds, _restOnStart,
                                                    RestFinished);
        WalkAction walkAction = new WalkAction(_patrol, _navigation);
        StopAction stopAction = new StopAction(_patrol, _navigation, inspection);
        InspectRoomAction inspectRoomAction = new InspectRoomAction(_patrol, inspection);
        AdvanceRoomAction advanceRoomAction = new AdvanceRoomAction(_patrol, _navigation);
        ReturnToStartAction returnToStartAction = new ReturnToStartAction(_patrol, _navigation, RestPointReached);

        // creación de la FSM de Nivel 2
        FSM movement = new FSM();

        // ESTADOS FSM NIVEL 2:
        State rest = CreateMovementState(movement, "Pausa", restAction.Begin, restAction.Tick);
        State walk = CreateMovementState(movement, "Andar", walkAction.Begin, walkAction.Tick);
        State stop = CreateMovementState(movement, "Parar", stopAction.Begin, stopAction.Tick);
        State checkRoom = CreateMovementState(movement, "ComprobarSala", inspectRoomAction.Begin, inspectRoomAction.Tick);
        State advanceRoom = CreateMovementState(movement, "AvanzarSala", advanceRoomAction.Begin, advanceRoomAction.Tick);
        State goHomeRoom = CreateMovementState(movement, "RetrocederSalaInicial", returnToStartAction.Begin, returnToStartAction.Tick);

        _inspectionState = checkRoom;   // para las animaciones

        // PERCEPCIONES FSM NIVEL 2:
        ConditionPerception inspectionPoint = new ConditionPerception(null, CheckPatrolPoint, null);
        ConditionPerception roomChangePending = new ConditionPerception(null, CheckRoomChangePending, null);
        ConditionPerception nextRoomPatrolPoint = new ConditionPerception(null, CheckPatrolPoint, null);
        ConditionPerception finalReturn = new ConditionPerception(null, CheckFinalReturn, null);

        // TRANSICIONES FSM NIVEL 2:
        // Andar -> Parar
        StateTransition walkToStop = movement.CreateTransition("EnPuntoDeInspeccion",
                            walk, stop, inspectionPoint, statusFlags: StatusFlags.Success);
        // Andar -> AvanzarSala
        StateTransition walkToAdvance = movement.CreateTransition("PuertaPendienteOSalaComprobada",
                            walk, advanceRoom, roomChangePending, statusFlags: StatusFlags.Success);
        // Parar -> ComprobarSala
        StateTransition stopToChek = movement.CreateTransition("EnAnguloDeComprobacion",
                            stop, checkRoom, statusFlags: StatusFlags.Success);
        // ComprobarSala -> Andar
        // en este caso se ejecuta aquí CompleteStep porque  es necesario actualizar el id del waypoint
        // cuando ya se ha terminado de inspeccionar y justo antes de volver a andar, para evitar saltarse
        // algún punto
        StateTransition checkToWalk = movement.CreateTransition("ZonaComprobada",
                            checkRoom, walk, action: new FunctionalAction(_patrol.CompleteStep, null, null), statusFlags: StatusFlags.Success);
        // AvanzarSala -> Andar
        StateTransition advanceToWalk = movement.CreateTransition("SiguienteSalaConPuntosPendientes",
                            advanceRoom, walk, nextRoomPatrolPoint, statusFlags: StatusFlags.Success);
        // AvanzarSala -> RetrocederSalaInicial
        StateTransition advanceToHome = movement.CreateTransition("RutaComprobada",
                            advanceRoom, goHomeRoom, finalReturn, statusFlags: StatusFlags.Success);
        // RetrocederSalaInicial -> Pausa
        StateTransition HomeToRest = movement.CreateTransition("EnSalaInicial",
                            goHomeRoom, rest, statusFlags: StatusFlags.None);
        // Pausa -> Andar
        StateTransition restToWalk = movement.CreateTransition("DescansoTerminado",
                                            rest, walk, statusFlags: StatusFlags.None);

        // PERCEPCIONES PUSH FSM NIVEL 2:
        // el guardia llega al punto de descanso
        _enterRest = new PushPerception(HomeToRest);
        // se agota el tiempo de descanso
        _leaveRest = new PushPerception(restToWalk);

        // se establece el estado inicial
        movement.SetEntryState(rest);

        return movement;
    }

    // Crea los estados de la FSM de Nivel 2: Movimiento
    private State CreateMovementState(FSM movement, string name, System.Action startMethod,
        Func<Status> updateMethod)
    {
        MovementStateEntry entry = new MovementStateEntry(EnterMovementState, startMethod);
        // se crea el estado
        State state = movement.CreateState(name, new FunctionalAction(entry.Start, updateMethod, null));
        // y se guarda su referencia
        entry.State = state;

        return state;
    }
    #endregion

    #region Auxiliar
    // Inicia o reanuda la acción del estado donde está el guardia (así luego se puede reanudar
    // esa acción a la mitad cuando se interrumpe por el jugador)
    private void EnterMovementState(State state, System.Action startMethod)
    {
        Debug.Log($"[Guardia] Estado de {startMethod.Method.DeclaringType.Name} | " +
                $"Ruta lista = {_patrol.IsReady} | Time Scale = {Time.timeScale}", this);

        // se guarda el estado para luego saber a cuál volver
        _activeMovementState = state;

        // en caso de tener que reanudar la acción
        if (_resumeMovement)
        {
            _resumeMovement = false;
            _navigation.Resume();
            return;
        }

        startMethod();
    }

    // Interrumpe la acción del guardia cuando ve al jugador
    private void SuspendMovement()
    {
        // se guarda el estado y la acción que estuviera realizando el guardia, para
        // luego poder reanudarla
        _movement.SetEntryState(_activeMovementState);
        _resumeMovement = true;
        _navigation.Suspend();
    }

    // Envía la alerta al monstruo de que el guardia ha visto al jugador
    // (estado MandarMonstruo)
    private void BeginAlert()
    {
        Debug.Log($"[Guardia] Jugador detectado en sala {CurrentRoomID}", this);

        // se interrumpe la acción
        SuspendMovement();
        _sendAlert.Begin();
    }

    // Indica que la alerta ha sido enviada (estado MandarMonstruo)
    private Status UpdateAlert()
    {
        return Status.Success;
    }

    // Comprueba si se ha visto al jugador en la sala (percepción playerDetected)
    private bool CheckPlayerDetected()
    {
        return _vision.DetectPlayer(CurrentRoomID);
    }

    // Comprueba el waypoint de destino (percepciones inspectionPoint y nextRoomPatrolPoint)
    private bool CheckPatrolPoint()
    {
        return _patrol.AtPatrolPoint;
    }

    // Comprueba la sala de destino (percepción roomChangePending)
    private bool CheckRoomChangePending()
    {
        return !_patrol.AtPatrolPoint;
    }

    // Comprueba si la ruta del guardia se dirige de nuevo a la primera sala,
    // es decir, la sala de descanso (percepción finalReturn)
    private bool CheckFinalReturn()
    {
        return _patrol.AtFinalReturn;
    }

    // Indica cuando el guardia llega al punto de descanso (acción returnAction)
    private void RestPointReached()
    {
        _pendingRestSignal = _enterRest;
    }

    // Indica cuando se agota el tiempo de descanso (acción restAction)
    private void RestFinished()
    {
        _pendingRestSignal = _leaveRest;
    }

    // Actualiza las animaciones del guardia
    private void UpdateAnimations()
    {
        if (_animator == null)
            return;

        // está andando?
        bool walking = !_resumeMovement && _navigation != null && _navigation.IsWalking;
        // está inspeccionando?
        bool inspecting = !_resumeMovement && _inspectionState != null && _activeMovementState == _inspectionState;

        _animator.SetBool("Andar", walking);
        _animator.SetBool("ComprobarSala", inspecting);
    }

    // Para debug (se dibuja la "visión" del guardia)
    private void OnDrawGizmosSelected()
    {
        MuseumGuardVision.DrawVisionGizmo(transform, _visionOrigin, _visionRange, _visionAngle);
    }
    #endregion
}
