using UnityEngine;

[RequireComponent(typeof(BoxCollider))]

// Clase que detecta cuando un jugador está delante de una salida de emergencia.
public class EmergencyDoorSensor : MonoBehaviour
{
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private PlayerHands _playerHands;
    [SerializeField] private SecurityDoor _door;

    /*
    private void OnTriggerEnter(Collider other)
    {
        TryOpen(other);
    }*/

    // Comprueba si el jugador está delante de una puerta de salida
    private void OnTriggerStay(Collider other)
    {
        TryOpen(other);
    }

    // Intenta abrir una puerta de salida cuando el jugador se coloca delante de ella
    // y ya se han desbloqueado
    private void TryOpen(Collider other)
    {
        if (_gameManager == null || !_gameManager.IsPlaying
            || !_gameManager.AreExitsUnlocked || _playerHands == null
            || !_playerHands.isActiveAndEnabled || _door == null
            || !_door.isActiveAndEnabled || Time.timeScale <= 0f)
            return;

        // una vez desbloqueadas las puertas de emergencia, se abren cuando el jugador
        // se coloca delante de ellas, para dejar salir a los niños
        PlayerHands enteringPlayer = other.GetComponentInParent<PlayerHands>();
        if (enteringPlayer == _playerHands)
            _door.Open();
    }
}
