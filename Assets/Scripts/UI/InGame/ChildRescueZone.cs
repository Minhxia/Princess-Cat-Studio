using UnityEngine;

[RequireComponent(typeof(BoxCollider))]

// Clase que representa la zona de salida del museo.
public class ChildRescueZone : MonoBehaviour
{
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private PlayerHands _playerHands;
    [SerializeField] private ChildCompanion[] _children = new ChildCompanion[0]; // array con los niños

    // Comprueba si el jugador lleva a algún niño de la mano cuando se detecta que
    // ha entrado en la zona de salida
    /*private void OnTriggerEnter(Collider other)
    {
        TryRescueChildren(other);
    }*/

    // Comprueba si el jugador lleva a algún niño de la mano estando ya dentro de la
    // zona de salida
    private void OnTriggerStay(Collider other)
    {
        TryRescueChildren(other);
    }

    // Intenta rescatar a un niño
    private void TryRescueChildren(Collider other)
    {
        if (_gameManager == null)
            _gameManager = GameManager.Instance;

        if (_gameManager == null || _playerHands == null || !_gameManager.IsPlaying
                || !_gameManager.AreExitsUnlocked)
            return;

        // solo el jugador puede activar la zona para rescatar a un niño
        // si entra el guardia, un monstruo o un niño solo, no vale
        PlayerHands enteringPlayer = other.GetComponentInParent<PlayerHands>();
        if (enteringPlayer != _playerHands)
            return;

        // en caso de que lleva más de un niño de la mano
        foreach (ChildCompanion child in _children)
        {
            if (child != null && _playerHands.IsAccompanying(child))
                // se registra al niño
                _gameManager.TryRescueChild(child);
        }
    }
}
