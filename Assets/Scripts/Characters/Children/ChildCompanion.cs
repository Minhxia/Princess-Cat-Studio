using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]

// Clase que controla lo que hace un niño desde que el jugador le coge de la mano
public class ChildCompanion : MonoBehaviour
{
    #region Variables
    [SerializeField] private PlayerHands _playerHands;

    [Header("Follow points")]
    [SerializeField] private Transform _leftFollowPoint;
    [SerializeField] private Transform _backLeftFollowPoint;
    [SerializeField] private Transform _rightFollowPoint;
    [SerializeField] private Transform _backRightFollowPoint;

    [Header("Distance")]
    [SerializeField, Min(0.1f)] private float _takeHandDistance = 1.3f;
    [SerializeField, Min(0.1f)] private float _maxSeparation = 2f;

    [Header("Speed")]
    [SerializeField, Range(0.1f, 1f)] private float _walkSpeedMultiplier = 1f;
    [SerializeField, Min(1f)] private float _waitingTurnSpeed = 360f;   // velocidad a la que se gira a mirar al jugador
    [SerializeField] private bool _blocksRunning = true; // si lleva a un niño de la mano no puede correr

    private const float PathUpdateInterval = 0.15f; // cada cuánto se calcula la ruta del niño
    private const float NavMeshSampleDistance = 0.35f;  // busca un punto hasta cierto límite alrededor del jugador

    private NavMeshAgent _agent;
    private CapsuleCollider _interactionCollider;
    private CharacterMovement3D _playerMovement;
    private NavMeshPath _path;
    private float _nextPathUpdate;
    private bool _previousUpdateRotation;
    #endregion

    #region Getters&Properties
    // indica si el jugador ya lleva de la mano al niño (para no cogerlo otra vez)
    private bool IsAccompanying => _playerHands != null && _playerHands.IsAccompanying(this);

    private bool AgentReady => _agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh;
    private float DistanceToPlayer => Vector3.Distance(transform.position, _playerHands.transform.position);

    public float WalkSpeedMultiplier => _walkSpeedMultiplier;
    public bool BlocksRunning => _blocksRunning;

    // indica si tiene un punto desde el que seguir al jugador (a su lado, o por detrás)
    private bool HasFollowPoints =>
        (_leftFollowPoint != null || _backLeftFollowPoint != null) &&
        (_rightFollowPoint != null || _backRightFollowPoint != null);

    // indica si se le puede dar la mano al niño
    private bool CanOfferHand => isActiveAndEnabled && _playerHands != null && _playerHands.isActiveAndEnabled
        && _interactionCollider != null && _interactionCollider.enabled
        && AgentReady && HasFollowPoints;

    // mensaje que se mostrará al jugador cuando se encuentre a un niño
    public string HandPrompt
    {
        get
        {
            if (!CanOfferHand)
                return "Can't take their hand";

            if (DistanceToPlayer > _takeHandDistance)
                return "Move closer to " + gameObject.name;

            // si ya lleva un niño en cada mano (dos a la vez), tiene que soltar primero
            // a uno de ellos
            if (_playerHands.BothHandsHaveChildren)
                return "Free a hand first";

            if (_playerHands.HasFreeHand)
                return "[F] Take hand";

            // en caso de llevar un niño de una mano y objeto recogible en la otra,
            // tirará automáticamente el objeto si le da la mano a otro niño
            string hand = _playerHands.PickupUsesLeftHand ? "left" : "right";
            return $"[F] Take hand\n<size=75%>Drops {hand}-hand item</size>";
        }
    }
    #endregion

    #region Unity
    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _interactionCollider = GetComponent<CapsuleCollider>();
        _path = new NavMeshPath();

        if (_playerHands != null)
            _playerMovement = _playerHands.GetComponent<CharacterMovement3D>();
    }

    // Actualiza el seguimiento del niño
    private void LateUpdate()
    {
        if (!IsAccompanying)
            return;

        // se comprueba la separación entre el jugador y el niño
        float separationLimit = Mathf.Max(_maxSeparation, _takeHandDistance);
        // si está muy lejos, le suelta la mano
        if (!AgentReady || DistanceToPlayer > separationLimit)
        {
            ReleaseHand();
            return;
        }

        // se comprueba si el jugador se ha llegado a desplazar, o solo se ha girado
        // sobre sí mismo antes de recalcular el nuevo punto y mover al niño
        // si están parados, el niño se gira a mirar al jugador
        if (_playerMovement == null || !_playerMovement.HasMovementInput)
        {
            _agent.isStopped = true;
            _agent.updateRotation = false;
            _nextPathUpdate = Time.time;

            // se calcula el vector para mirar en la dirección correspondiente
            Vector3 directionToPlayer = _playerHands.transform.position - transform.position;
            directionToPlayer.y = 0f;

            // se gira al niño
            if (directionToPlayer.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    _waitingTurnSpeed * Time.deltaTime
                );
            }

            return;
        }
        // se mantiene su rotación a dónde estuviera mirando antes
        _agent.updateRotation = _previousUpdateRotation;

        if (Time.time < _nextPathUpdate)
            return;
        // siguiente actualización del camino
        _nextPathUpdate = Time.time + PathUpdateInterval;

        // en función de qué mano esté libre, el niño se colocará a un lado
        // o al otro del jugador, o por detrás
        bool isInLeftHand = _playerHands.IsChildInLeftHand(this);
        // se elige el punto
        Transform sidePoint = isInLeftHand ? _leftFollowPoint : _rightFollowPoint;
        Transform backPoint = isInLeftHand ? _backLeftFollowPoint : _backRightFollowPoint;
        // intenta seguir al jugador desde uno de los puntos
        bool foundPath = TryFollow(sidePoint) || TryFollow(backPoint);
        _agent.isStopped = !foundPath;
    }

    private void OnDisable()
    {
        ReleaseHand();
    }
    #endregion

    #region Hands
    // Coge la mano del niño 
    public void TakeHand()
    {
        // el niño no le puede dar la mano al jugador si no tiene una mano libre
        // o está muy lejos
        if (!CanOfferHand || DistanceToPlayer > _takeHandDistance)
        {
            Debug.Log("[ChildCompanion] El niño no le puede dar la mano al jugador.");
            return;
        }

        // el jugador no tiene manos libres
        if (!_playerHands.TryReserveChildHand(this)) return;

        _previousUpdateRotation = _agent.updateRotation;
        _interactionCollider.enabled = false;
        _nextPathUpdate = Time.time; // la actualización del camino toma el tiempo actual
    }
    // Suelta la mano del niño de la del jugador
    public void ReleaseHand()
    {
        if (!IsAccompanying)
            return;

        // se quita la referencia del niño de las manos del jugador
        _playerHands.ClearChildReservation(this);

        // se para su movimiento
        if (AgentReady)
            _agent.isStopped = true;

        // mira a donde quiera
        if (_agent != null)
            _agent.updateRotation = _previousUpdateRotation;

        // y se activa su collider para poder interactuar de nuevo con el niño
        if (_interactionCollider != null)
            _interactionCollider.enabled = true;
    }
    #endregion

    #region Path
    // Indica si el niño tiene un camino desde el punto elegido (al lado, detrás)
    // para seguir al jugador
    private bool TryFollow(Transform point)
    {
        if (point == null)
            return false;

        NavMeshQueryFilter filter = new NavMeshQueryFilter
        {
            agentTypeID = _agent.agentTypeID, // tipo de agente
            areaMask = _agent.areaMask        // áreas que puede utilizar
        };

        // se busca un camino, un punto, alrededor del jugador para seguirle
        if (!NavMesh.SamplePosition(point.position, out NavMeshHit hit, NavMeshSampleDistance, filter))
            return false;

        // se comprueba si puede llegar al punto
        if (!_agent.CalculatePath(hit.position, _path) || _path.status != NavMeshPathStatus.PathComplete)
            return false;

        // se asigna el camino calculado y se devuelve true
        return _agent.SetPath(_path);
    }
    #endregion
}
