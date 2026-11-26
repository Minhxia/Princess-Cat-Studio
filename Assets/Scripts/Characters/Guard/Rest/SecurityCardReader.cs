using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(InteractableItem))]

// Clase que representa el panel de seguridad de la puerta de la garita, y su funcionamiento.
public class SecurityCardReader : MonoBehaviour
{
    #region Variables
    // tipo de panel de seguridad en función de la puerta
    public enum ReaderMode
    {
        SecurityRoom,   // garita de seguridad 
        EmergencyExit   // salida de emergencia
    }

    [SerializeField] private ReaderMode _readerMode = ReaderMode.SecurityRoom;

    [Header("References")]
    [SerializeField] private SecurityCard _acceptedCard;
    [SerializeField] private SecurityDoor _door;
    [SerializeField] private EmergencyExitDoor _emergencyDoor;
    [SerializeField] private ExitFeedbackUI _feedback;
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private PlayerHands _playerHands;
    [SerializeField, Min(0.1f)] private float _useDistance = 5f;  // distancia máxima para usar el lector


    // para posible feedback (sonido, texto, etc.):
    [SerializeField] private UnityEvent _onAccepted = new UnityEvent();
    [SerializeField] private UnityEvent _onDenied = new UnityEvent();
    #endregion

    // Comprueba si es el jugador quién está utilizando la tarjeta en el lector
    public void UseByPlayer()
    {
        if (_playerHands == null || !_playerHands.isActiveAndEnabled || Time.timeScale <= 0f)
            return;

        SecurityCard card = null;
        // se busca en qué mano lleva la tarjeta
        // izda
        if (_playerHands.LeftItem != null)
            card = _playerHands.LeftItem.GetComponent<SecurityCard>();
        // dcha (si la lleva en la izda se salta esta comprobación)
        if (card != _acceptedCard && _playerHands.RightItem != null)
            card = _playerHands.RightItem.GetComponent<SecurityCard>();

        // el lector intentar leer la tarjeta
        TryRead(card, _playerHands.transform);
    }

    // Intenta leer la tarjeta de seguridad para abrir la puerta correspondiente
    public bool TryRead(SecurityCard card, Transform user)
    {
        // primero se comprueba si se puede usar
        if (user == null || Vector3.Distance(user.position, transform.position) > _useDistance)
        {
            DenyAccess();
            return false;
        }

        // las salidas de emergencia se desbloquean una vez para toda la partida
        if (_readerMode == ReaderMode.EmergencyExit)
        {
            if (_gameManager == null || !_gameManager.IsPlaying
       || _emergencyDoor == null || !_emergencyDoor.isActiveAndEnabled)
            {
                DenyAccess();
                return false;
            }

            // si la puerta ya está desbloqueada, no se pide la tarjeta otra vez
            if (_emergencyDoor.IsUnlocked)
                return true;

            if (!HasAcceptedCard(card, user) || !_emergencyDoor.Unlock())
            {
                DenyAccess();
                return false;
            }

            // se muestra el feedback solo la primera vez que se desbloquea la puerta
            if (_feedback != null)
                _feedback.ShowExitUnlocked();
            else
                Debug.LogWarning(
                    "[SecurityCardReader] Falta asignar ExitFeedbackUI.",
                    this);

            _onAccepted.Invoke();
            return true;
        }

        // aa puerta de la garita simplemente se bloquea y desbloquea cada vez
        if (_door == null || !_door.isActiveAndEnabled
            || !HasAcceptedCard(card, user))
        {
            DenyAccess();
            return false;
        }

        // se abre la puerta
        _door.Open();
        _onAccepted.Invoke();
        return true;
    }

    // Indica si el jugador lleva una tarjeta válida
    private bool HasAcceptedCard(SecurityCard card, Transform user)
    {
        return card != null && card == _acceptedCard && card.IsCarriedBy(user);
    }

    // Indica que se ha denegado el acceso (la tarjeta no sirve)
    private void DenyAccess()
    {
        _onDenied.Invoke();
        Debug.Log("[SecurityCardReader] Acceso denegado.", this);
    }
}
