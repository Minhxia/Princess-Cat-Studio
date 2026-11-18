using System.Collections.Generic;
using UnityEngine;
using Door = MuseumGuardPatrolRunner.Door;
using PatrolPoint = MuseumGuardPatrolRunner.PatrolPoint;
using Room = MuseumGuardPatrolRunner.Room;

// Clase que gestiona la ruta del guardia durante su patrulla.
public class MuseumGuardPatrolRoute
{
    #region Variables
    public const int HomeRoomID = 12;   // sala de seguridad y tienda de regalos

    private enum StepKind { PatrolPoint, Door, Backtrack, Rest }

    private readonly struct Step
    {
        public readonly StepKind Kind;      // tipo de paso (patrulla, puerta, vuelta, descanso)
        public readonly Transform Target;   // posición donde está
        public readonly int RoomOnArrival;  // id de la sala
        public readonly float WaitSeconds;  // tiempo de la inspección

        public Step(StepKind kind, Transform target, int roomOnArrival, float waitSeconds = 0f)
        {
            Kind = kind;
            Target = target;
            RoomOnArrival = roomOnArrival;
            WaitSeconds = waitSeconds;
        }
    };

    private readonly List<Step> _steps = new List<Step>();  // lista de pasos (ruta)
    private int _stepIndex;         // id del paso
    private int _firstReturnStep;   // id del primer paso de la vuelta a la sala inicial
    #endregion

    #region Getters
    public bool IsReady { get; }
    public int CurrentRoomID { get; private set; } = HomeRoomID;
    public bool HasPendingStep => _stepIndex < _steps.Count;
    public bool AtPatrolPoint => HasPendingStep && CurrentStep.Kind == StepKind.PatrolPoint;
    public bool AtFinalReturn => _stepIndex >= _firstReturnStep;
    public Transform CurrentTarget => CurrentStep.Target;
    public float CurrentWaitSeconds => CurrentStep.WaitSeconds;

    private Step CurrentStep => _steps[_stepIndex];
    #endregion

    #region Methods
    public MuseumGuardPatrolRoute(Room[] rooms, Transform restPoint)
    {
        IsReady = BuildPatrol(rooms, restPoint);
    }

    // Resetea la ruta (se vuelve al punto de descanso)
    public void Reset()
    {
        _stepIndex = 0;
        CurrentRoomID = HomeRoomID;
    }

    // Completa un paso
    public void CompleteStep()
    {
        CurrentRoomID = CurrentStep.RoomOnArrival;
        _stepIndex++;
    }
    #endregion

    #region Builders
    // Crea la ruta
    private bool BuildPatrol(Room[] rooms, Transform restPoint)
    {
        // se crea un diccionario con todas las salas y sus ids
        Dictionary<int, Room> roomsByID = new Dictionary<int, Room>();
        foreach (Room room in rooms)
        {
            if (room == null || room.id < 0 || roomsByID.ContainsKey(room.id))
            {
                Debug.LogError("[MuseumGuardPatrolRoute] Cada sala debe tener un id único y positivo.");
                return false;
            }
            // se añade la sala
            roomsByID.Add(room.id, room);
        }

        // se comprueba que existe el id de la sala inicial (sala de seguridad + tienda de regalos)
        if (!roomsByID.TryGetValue(HomeRoomID, out Room home))
        {
            Debug.LogError("[MuseumGuardPatrolRoute] Falta añadir la sala inicial.");
            return false;
        }

        // se crea una lista de los puntos por los que ya haya pasado
        List<int> visited = new List<int>();
        if (!AddRoomSteps(home, roomsByID, visited))
        {
            return false;
        }
        // y se comprueba que contenga el mismo número de puntos
        if (visited.Count != roomsByID.Count)
        {
            Debug.LogError("[MuseumGuardPatrolRoute] Hay salas sin conectar en el árbol, que empieza "
                                + "en la sala 12.");
            return false;
        }

        // una vez se tiene la ruta, se elige el punto de descanso
        Transform homeTarget = restPoint;
        PatrolPoint[] homePoints = home.points;
        // en caso de no tener punto de descanso, coge su primer waypoint como zona de descanso
        if (homeTarget == null && homePoints.Length > 0)
            homeTarget = homePoints[0].target;
        // si no tiene ni punto de descanso ni waypoints, da error
        if (homeTarget == null)
        {
            Debug.LogError("[MuseumGuardPatrolRoute] Hay que asignar Rest Point o añadir al menos un "
                                + "waypoint en la sala 12.");
            return false;
        }
        // y si tiene punto de descanso, pero no hay waypoints, no se puede mover
        // de donde está, así que no puede salir a patrullar
        if (_steps.Count == 0)
        {
            Debug.LogError("[MuseumGuardPatrolRoute] Hay que añadir un waypoint o puerta a la patrulla.");
            return false;
        }

        // se busca y guarda el primer paso para la VUELTA a la sala inicial
        _firstReturnStep = _steps.Count;
        // para ello, hay que buscar en la lista los pasos de tipo Backtrack, hasta dar con uno
        // distinto por ejemplo PatrolPoint, y así saber cuál es el primero de esa VUELTA
        while (_firstReturnStep > 0 && _steps[_firstReturnStep - 1].Kind == StepKind.Backtrack)
        {
            _firstReturnStep--;
        }
        // finalmente, se añade el punto de descanso como último punto de destino
        _steps.Add(new Step(StepKind.Rest, homeTarget, HomeRoomID));

        return true;
    }

    // Añade los waypoints de una sala (se empieza por la sala inicial = 12)
    private bool AddRoomSteps(Room room, Dictionary<int, Room> roomsByID, List<int> visited)
    {
        // primero se comprueba que la sala dada no esté ya en la lista
        if (visited.Contains(room.id))
        {
            Debug.LogError("[MuseumGuardPatrolRoute] La sala " + room.id + " aparece más de una vez en la lista.");
            return false;
        }
        // si no está, se añade
        visited.Add(room.id);

        PatrolPoint[] points = room.points;
        // se calcula dónde añadir las puertas (su id)
        int leftAfter = DoorAfter(room.leftDoor, defaultInsertionIndex: 0);
        int rightAfter = DoorAfter(room.rightDoor, defaultInsertionIndex: points.Length);
        if (leftAfter > points.Length || rightAfter > points.Length)
        {
            Debug.LogError("[MuseumGuardPatrolRoute] After point de la sala " + room.id + " está fuera de la lista de puntos.");
            return false;
        }
        // en caso de que el orden de las puertas en la lista no sea correcto
        if (leftAfter >= 0 && rightAfter >= 0 && rightAfter < leftAfter)
        {
            Debug.LogError("[MuseumGuardPatrolRoute] En la sala " + room.id + ", la puerta izda debe ir antes que la dcha.");
            return false;
        }

        // se recorren los waypoins y se van intercalando las puertas donde corresponda, y se obtiene
        // un ruta del tipo: WA -> P12 -> P7 -> WC -> P7 -> P12 -> WB
        for (int nextPoint = 0; nextPoint <= points.Length; nextPoint++)
        {
            // en caso de que alguna puerta no se pueda añadir
            if (leftAfter == nextPoint && !AddDoorSteps(room, room.leftDoor, roomsByID, visited))
            {
                return false;
            }
            if (rightAfter == nextPoint && !AddDoorSteps(room, room.rightDoor, roomsByID, visited))
            {
                return false;
            }
            // si se alcanza el final del array, se sale
            if (nextPoint == points.Length)
                break;

            // se comprueba el punto
            PatrolPoint point = points[nextPoint];
            // si no tiene una posición
            if (point.target == null)
            {
                Debug.LogError("[MuseumGuardPatrolRoute] Falta el target en el punto " + nextPoint
                                    + " de la sala " + room.id);
                return false;
            }
            // finalmente, se añade el waypoint
            _steps.Add(new Step(StepKind.PatrolPoint, point.target, room.id, point.waitSeconds));
        }
        // y por fin todo fue bien :')
        return true;
    }

    // Indica el id en el que debe ir una puerta (un cruce entre salas)
    private int DoorAfter(Door door, int defaultInsertionIndex)
    {
        // con una ruta A -> B
        // si es = -1 -> se usa la posición por defecto:
        // puerta izda = -1 -> va antes de WA, puerta dcha = -1 -> va después de WB
        if (door == null || door.childRoomID < 0)
        {
            return -1;
        }

        // si una puerta = 0 -> va entre WA y WB, si puerta = 1 -> va después de WB,
        // y si puerta = 2 -> error!
        return door.afterPoint < 0 ? defaultInsertionIndex : door.afterPoint + 1;
    }

    // Añade las puertas entre salas
    private bool AddDoorSteps(Room parent, Door door, Dictionary<int, Room> roomsByID, List<int> visited)
    {
        // primero se busca la sala a la que va a dar la puerta (sala padre -> puerta -> sala hija)
        if (!roomsByID.TryGetValue(door.childRoomID, out Room child))
        {
            Debug.LogError("[MuseumGuardPatrolRoute] La puerta de la sala " + parent.id + " da a una sala que no existe "
                                + door.childRoomID);
            return false;
        }
        // se comprueba que hay posiciones a ambos lados (en la sala padre y en la hija)
        if (door.parentSide == null || door.childSide == null)
        {
            Debug.LogError("[MuseumGuardPatrolRoute] Hay que asignar los dos puntos a cada lado de la puerta entre las salas "
                                + parent.id + " y " + child.id);
            return false;
        }
        // si todo va bien, se añaden los puntos
        _steps.Add(new Step(StepKind.Door, door.parentSide, parent.id));
        _steps.Add(new Step(StepKind.Door, door.childSide, child.id));
        // se añade la sala hija (llamada recursiva)
        if (!AddRoomSteps(child, roomsByID, visited))
            return false;

        // finalmente, se añade el mismo camino pero al revés para la vuelta
        _steps.Add(new Step(StepKind.Backtrack, door.childSide, child.id));
        _steps.Add(new Step(StepKind.Backtrack, door.parentSide, parent.id));

        return true;
    }
    #endregion
}
