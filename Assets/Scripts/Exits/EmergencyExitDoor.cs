using UnityEngine;

[RequireComponent(typeof(Animator), typeof(BoxCollider), typeof(Rigidbody))]

// Clase que gestiona la apertura de las puertas de salida.
public class EmergencyExitDoor : MonoBehaviour
{
    #region Variables
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private Collider _playerCollider;

    private Animator _animator;
    private bool _wantsOpen;
    private bool _fullyOpen;
    #endregion

    public bool IsFullyOpen => _fullyOpen; // indica si la puerta está abierta por completo

    #region Unity
    private void Awake()
    {
        // se guardan las referencias
        _animator = GetComponent<Animator>();
        _animator.SetBool("Open", false);

        BoxCollider approachTrigger = GetComponent<BoxCollider>();
        approachTrigger.isTrigger = true;

        Rigidbody body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
    }

    // Apertura de la puerta
    private void OnTriggerEnter(Collider other)
    {
        TryOpenForPlayer(other);
    }

    // Apertura de la puerta
    private void OnTriggerStay(Collider other)
    {
        // en caso de que el jugador ya esté delante cuando desbloquee la salida
        TryOpenForPlayer(other);
    }

    // Cierre de la puerta
    private void OnTriggerExit(Collider other)
    {
        if (other == _playerCollider)
            SetOpen(false);
    }
    #endregion

    // Comprueba si se puede abrir la puerta
    private void TryOpenForPlayer(Collider other)
    {
        if (other != _playerCollider || _gameManager == null)
            return;

        if (_gameManager.IsPlaying && _gameManager.AreExitsUnlocked)
            SetOpen(true);
    }

    // Abre la puerta
    private void SetOpen(bool open)
    {
        if (_wantsOpen == open)
            return;

        _wantsOpen = open;

        if (!open)
            _fullyOpen = false;

        _animator.SetBool("Open", open);
    }

    #region Events
    // Evento de animación para el final del clip de apertura
    public void OnFinishedOpening()
    {
        _fullyOpen = _wantsOpen && _gameManager != null && _gameManager.AreExitsUnlocked;
    }

    // Evento de animación para el final del clip de cierre
    public void OnFinishedClosing()
    {
        _fullyOpen = false;
    }
    #endregion
}
