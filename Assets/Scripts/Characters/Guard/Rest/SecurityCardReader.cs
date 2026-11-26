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
    [SerializeField] private SecurityCard _acceptedCard;
    [SerializeField] private SecurityDoor _door;
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private PlayerHands _playerHands;
    [SerializeField, Min(0.1f)] private float _useDistance = 2.5f;  // distancia máxima para usar el lector

    // para posible feedback (sonido, texto, etc.):
    [SerializeField] private UnityEvent _onAccepted = new UnityEvent();
    [SerializeField] private UnityEvent _onDenied = new UnityEvent();
    #endregion

    // Comprueba si es el jugador quién está utilizando la tarjeta en el lector
    public void UseByPlayer()
    {
        //Debug.Log("[Panel] Se ha llamado a UseByPlayer.", this);

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

    // Intenta leer la tarjeta de seguridad para abrir la puerta
    public bool TryRead(SecurityCard card, Transform user)
    {
        // para las puertas de las salidas de emergencia
        bool exitsUnlocked = _readerMode == ReaderMode.EmergencyExit && _gameManager.AreExitsUnlocked;
        // para la puerta de la garita de seguridad
        bool hasCard = card != null && card == _acceptedCard && card.IsCarriedBy(user);

        // primero se comprueba que hay una tarjeta, que es válida para el lector,
        // quién la quiere utilizar y su distancia al lector
        // además se diferencia qué tipo de puerta es
        bool valid = user != null && Vector3.Distance(user.position, transform.position) <= _useDistance
                        && _door != null && _door.isActiveAndEnabled
                        && (hasCard || exitsUnlocked);

        // en caso de ser un panel de una puerta de salida, también se comprueba que
        // se esté jugando
        if (_readerMode == ReaderMode.EmergencyExit)
            valid = valid && _gameManager.IsPlaying;

        // si no se cumple lo necesario para un tipo de puerta, se deniega el acceso
        if (!valid)
        {
            _onDenied.Invoke();
            Debug.Log("[SecurityCardReader] Acceso denegado.", this);
            return false;
        }

        // se comprueba si se pueden desbloquear las salidas de emergencia
        if (_readerMode == ReaderMode.EmergencyExit && !_gameManager.UnlockExits())
        {
            _onDenied.Invoke();
            return false;
        }

        // si todo está bien, se abre la puerta
        _door.Open();
        _onAccepted.Invoke();
        return true;
    }
}
