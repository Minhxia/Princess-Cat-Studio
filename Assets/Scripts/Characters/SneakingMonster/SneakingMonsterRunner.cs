using BehaviourAPI.Core;
using BehaviourAPI.Core.Actions;
using BehaviourAPI.Core.Perceptions;
using BehaviourAPI.StateMachines;
using BehaviourAPI.UnityToolkit;
using UnityEngine;
using UnityEngine.AI;

// Clase que crea la máquina de estados del monstruo que se mueve cuando el jugador no lo mira.
// (las acciones están dentro de los estados)
[RequireComponent(typeof(NavMeshAgent))]

public class SneakingMonsterRunner : BehaviourRunner
{
    #region Variables
    [Header("References")]
    [SerializeField] private Transform _player;
    [SerializeField] private Camera _playerCamera;
    [SerializeField] private Transform _visionTarget; // punto del monstruo que mira la cámara del jugador

    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float _zoneRadius = 12f;
    [SerializeField, Min(0.1f)] private float _moveSpeed = 3f;
    [SerializeField, Min(0.05f)] private float _repathSeconds = 0.2f;
    [SerializeField, Min(0.05f)] private float _homeTolerance = 0.3f;  // para cuando el jugador se sale de su zona

    [Header("Vision")]
    [SerializeField, Min(0.1f)] private float _maxViewDistance = 18f;
    // paredes, objetos, etc. que impidan la visión
    [SerializeField] private LayerMask _obstacleLayers;
    // tiempo que pasa para volver a la posición inicial si deja de ver al jugador
    [SerializeField, Min(0f)] private float _lostSightSeconds = 0.35f;

    private NavMeshAgent _agent;
    private Vector3 _homePosition; // posición a la que vuelve después de perseguir al jugador
    private bool _playerInContact; // indica si el trigger está tocando al jugador
    private float _lastSawPlayerTime = float.NegativeInfinity;
    #endregion

    #region FSM
    // Crea la máquina de estados de un nivel
    protected override BehaviourGraph CreateGraph()
    {
        // referencias
        _agent = GetComponent<NavMeshAgent>();
        _homePosition = transform.position;

        // comprobaciones
        if (_playerCamera == null && _player != null)
            _playerCamera = _player.GetComponentInChildren<Camera>();

        if (_player == null || _playerCamera == null)
        {
            Debug.LogError("[SneakingMonster] Hay que asignar Player y su cámara en el Inspector.", this);
            return null;
        }

        if (_obstacleLayers.value == 0)
            Debug.LogWarning("[SneakingMonster] Hay que asignar las capas de paredes en Obstacle Layers.", this);

        // ACCIONES:
        MonsterWaitAction waitAction = new MonsterWaitAction(_agent);
        MonsterChaseAction chaseAction = new MonsterChaseAction(_agent, _player, _moveSpeed,
                                                                    _repathSeconds);
        MonsterStopAction stopAction = new MonsterStopAction(_agent);
        MonsterReturnAction returnAction = new MonsterReturnAction(_agent, _homePosition,
                                                                    _moveSpeed, _homeTolerance);
        MonsterKillAction killAction = new MonsterKillAction(_agent);

        // creación de la FSM
        FSM monster = new FSM();

		// ESTADOS:
		// --BACKUP--
		/*
        State wait = monster.CreateState("Esperar", new FunctionalAction(waitAction.Begin, waitAction.Tick, null));
        State chase = monster.CreateState("Perseguir", new FunctionalAction(chaseAction.Begin, chaseAction.Tick, null));
        State stop = monster.CreateState("Parar", new FunctionalAction(stopAction.Begin, stopAction.Tick, null));
        State returnHome = monster.CreateState("Reiniciar", new FunctionalAction(returnAction.Begin, returnAction.Tick, null));
        */

        // Aquí se dice lo mismo pero con dos acciones adicionales
		State wait = monster.CreateState("Esperar", new FunctionalAction(() =>
		{
			waitAction.Begin();
			notificarAudio(AudioManager.nivelAlertaEnemigos.Normal);
		}, waitAction.Tick, null));



		State chase = monster.CreateState("Perseguir", new FunctionalAction(() =>
		{
			chaseAction.Begin();
			notificarAudio(AudioManager.nivelAlertaEnemigos.Perseguir);
		}, chaseAction.Tick, null));


		State stop = monster.CreateState("Parar", new FunctionalAction(() =>
		{
			stopAction.Begin();
			notificarAudio(AudioManager.nivelAlertaEnemigos.Alerta);
		}, stopAction.Tick, null));

		State returnHome = monster.CreateState("Reiniciar", new FunctionalAction(() =>
		{
			returnAction.Begin();
			notificarAudio(AudioManager.nivelAlertaEnemigos.Normal);
		}, returnAction.Tick, null));

		State kill = monster.CreateState("Matar", new FunctionalAction(killAction.Begin, killAction.Tick, null));

		// PERCEPCIONES:
		ConditionPerception playerSeenInZone = new ConditionPerception(null, CheckPlayerSeenInZone, null);
        ConditionPerception playerUnseenInZone = new ConditionPerception(null, CheckPlayerUnseenInZone, null);
        ConditionPerception playerUnseenOutsideZone = new ConditionPerception(null, CheckPlayerUnseenOutsideZone, null);
        ConditionPerception monsterObserved = new ConditionPerception(null, CheckObserved, null);
        ConditionPerception playerContact = new ConditionPerception(null, CheckPlayerContact, null);
        ConditionPerception playerHiddenInZone = new ConditionPerception(null, CheckPlayerHiddenInZone, null);

        // TRANSICIONES:
        // Esperar -> Parar
        StateTransition waitToStop = monster.CreateTransition("JugadorEnZonaYLoVe", wait, stop,
                                                                    playerSeenInZone);
        // Esperar -> Perseguir
        StateTransition waitToChase = monster.CreateTransition("JugadorEnZonaYNoLoVe", wait, chase,
                                                                    playerUnseenInZone);
        // Perseguir -> Matar
        StateTransition chaseToKill = monster.CreateTransition("ContactoConJugador", chase, kill,
                                                                    playerContact);
        // Perseguir -> Parar
        StateTransition chaseToStop = monster.CreateTransition("JugadorLoObserva", chase, stop,
                                                                    monsterObserved);
        // Perseguir -> Reiniciar
        StateTransition chaseToHome = monster.CreateTransition("JugadorSaleDeZona", chase, returnHome,
                                                                    playerUnseenOutsideZone);

        // TRANSICIÓN ESPECIAL: porque el jugador se esconde detrás de un obstáculo
        monster.CreateTransition("JugadorSeEsconde", chase, returnHome, playerHiddenInZone);

        // Parar -> Perseguir
        StateTransition stopToChase = monster.CreateTransition("JugadorDejaDeObservarlo", stop, chase,
                                                                    playerUnseenInZone);
        // Parar -> Reiniciar
        StateTransition stopToHome = monster.CreateTransition("SaleDeZonaSinMirarlo", stop, returnHome,
                                                                    playerUnseenOutsideZone);

        // TRANSICIÓN ESPECIAL: porque el jugador se esconde detrás de un obstáculo
        monster.CreateTransition("SeOcultaSinMirarlo", stop, returnHome, playerHiddenInZone);

        // Reiniciar -> Parar / Perseguir / Esperar.
        StateTransition homeToStop = monster.CreateTransition("LoObservaAlVolver", returnHome, stop,
                                                                    monsterObserved);
        // Reiniciar -> Perseguir
        StateTransition homeToChase = monster.CreateTransition("EntraEnZonaAlVolver", returnHome, chase,
                                                                    playerUnseenInZone);
        // Reiniciar -> Esperar
        StateTransition homeToWait = monster.CreateTransition("LlegaAlInicio", returnHome, wait,
                                                                statusFlags: StatusFlags.Success);

        // se establece el estado inicial
        monster.SetEntryState(wait);

        return monster;
    }
    #endregion

    #region Auxiliar
    // Comprueba si el jugador se encuentra dentro de la zona de activación
    private bool CheckPlayerInZone()
    {
        if (_player == null)
            return false;

        // diferencia entre la posición del jugador y la zona del monstruo
        Vector3 difference = _player.position - _homePosition;
        difference.y = 0f;

        return difference.sqrMagnitude <= _zoneRadius * _zoneRadius;
    }

    // Comprueba si el jugador está en la zona y puede ver al monstruo
    private bool CheckPlayerSeenInZone()
    {
        return CheckPlayerInZone() && CheckObserved();
    }

    // Comprueba si el jugador está en la zona y no puede ver al monstruo,
    // pero el monstruo sí ve al jugador
    private bool CheckPlayerUnseenInZone()
    {
        return CheckPlayerInZone() && !CheckObserved() && CheckMonsterSeesPlayer();
    }

    // Comprueba si el jugador ha salido de la zona y ya no observa al monstruo
    private bool CheckPlayerUnseenOutsideZone()
    {
        return !CheckPlayerInZone() && !CheckObserved();
    }

    // Comprueba si el jugador  se oculta detrás de un obstáculo
    private bool CheckPlayerHiddenInZone()
    {
        if (!CheckPlayerInZone() || CheckObserved())
            return false;

        if (CheckMonsterSeesPlayer())
            return false;

        return Time.time - _lastSawPlayerTime >= _lostSightSeconds;
    }

    // Comprueba si una pared u objeto tapa al jugador y el monstruo no puede verlo
    private bool CheckMonsterSeesPlayer()
    {
        if (_playerCamera == null)
            return false;

        Vector3 origin = _visionTarget != null ? _visionTarget.position
                                                : transform.position + Vector3.up * 1.2f;

        Vector3 target = _playerCamera.transform.position;

        bool blocked = Physics.Linecast(origin, target, _obstacleLayers,
            QueryTriggerInteraction.Ignore);

        if (blocked)
            return false;

        _lastSawPlayerTime = Time.time;
        return true;
    }

    // Comprueba el encuadre de la cámara y si hay una pared entre ella y el monstruo.
    private bool CheckObserved()
    {
        if (_playerCamera == null)
            return false;

        Vector3 target = _visionTarget != null ? _visionTarget.position
                                               : transform.position + Vector3.up * 1.2f;

        Vector3 offset = target - _playerCamera.transform.position;
        if (offset.sqrMagnitude > _maxViewDistance * _maxViewDistance)
            return false;

        // la cámara determina el ángulo de visión (su FOV actual)
        Vector3 viewport = _playerCamera.WorldToViewportPoint(target);
        if (viewport.z <= 0f || viewport.x < 0f || viewport.x > 1f
            || viewport.y < 0f || viewport.y > 1f)
            return false;

        // se tienen en cuenta los objetos, paredes, etc., que tapan la visión
        return !Physics.Linecast(_playerCamera.transform.position, target,
            _obstacleLayers, QueryTriggerInteraction.Ignore);
    }

    // Comprueba si el trigger del monstruo ha tocado al jugador (sin una pared entre ellos)
    private bool CheckPlayerContact()
    {
        if (!_playerInContact || _player == null)
            return false;

        // se evita que no haya una derrota a través de una pared
        Vector3 from = transform.position + Vector3.up;
        Vector3 to = _player.position + Vector3.up;
        return !Physics.Linecast(from, to, _obstacleLayers,
            QueryTriggerInteraction.Ignore);
    }

    // Control del contacto con el jugador:
    private void OnTriggerEnter(Collider other)
    {
        UpdatePlayerContact(other);
    }

    private void OnTriggerStay(Collider other)
    {
        UpdatePlayerContact(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (_player != null && other.transform.IsChildOf(_player))
            _playerInContact = false;
    }

    private void UpdatePlayerContact(Collider other)
    {
        if (_player != null && other.transform.IsChildOf(_player))
            _playerInContact = true;
    }

    // DEBUG
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(Application.isPlaying ? _homePosition : transform.position,
            _zoneRadius);
    }
	private void notificarAudio(AudioManager.nivelAlertaEnemigos estado)
	{
		if (AudioManager.Instance != null)
		{
			AudioManager.Instance.enemyNewState(gameObject.GetEntityId(), estado);
		}
	}
	#endregion
}
