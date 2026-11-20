using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(InteractableItem))]

// Clase que representa el panel de seguridad de la puerta de la garita, y su funcionamiento.
public class SecurityCardReader : MonoBehaviour
{
    #region Variables
    [SerializeField] private SecurityCard _acceptedCard;
    [SerializeField] private SecurityDoor _door;
    [SerializeField] private PlayerHands _playerHands;
    [SerializeField, Min(0.1f)] private float _useDistance = 2.5f;  // distancia máxima para usar el lector
    
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

    // Intenta leer la tarjeta de seguridad
    public bool TryRead(SecurityCard card, Transform user)
    {
        // primero se comprueba que hay una tarjeta, que es válida para el lector
        // y quién la quiere utilizar
        bool valid = card != null && card == _acceptedCard && card.IsCarriedBy(user);
        // si eso es válido, se comprueba la distancia al lector
        valid = valid && Vector3.Distance(user.position, transform.position) <= _useDistance;
        // también se comprueba que hay una puerta
        valid = valid && _door != null && _door.isActiveAndEnabled;

        // si no se cumple todo, se deniega el acceso a la garita
        if (!valid)
        {
            _onDenied.Invoke();
            Debug.Log("[SecurityCardReader] Acceso denegado.", this);
            return false;
        }

        // si todo está bien, se abre la puerta
        _door.Open();
        _onAccepted.Invoke();
        return true;
    }
}
