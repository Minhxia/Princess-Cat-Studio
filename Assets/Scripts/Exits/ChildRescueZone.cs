using UnityEngine;

[RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]

// Clase que representa la zona de rescatre del museo (salidas de emergencia).
public class ChildRescueZone : MonoBehaviour
{
    #region Variables
    [Header("References")]
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private PlayerHands _playerHands;
    [SerializeField] private Collider _playerCollider;
    [SerializeField] private EmergencyExitDoor _door;
    [SerializeField] private ExitFeedbackUI _feedback;

    [Header("Children")]
    // array con los niños, para poder reconocerlos
    [SerializeField] private ChildCompanion[] _children = new ChildCompanion[0];

    private bool _playerInside; // indica si el jugador está dentro de la zona de rescate
    #endregion

    #region Getters
    public static ChildRescueZone ActiveZone { get; private set; } // zona de rescate

    // indica si se puede rescatar a los niños, es decir, si el jugador está en la zona
    // de rescate
    public bool CanRescue
    {
        get
        {
            if (!_playerInside || _gameManager == null || _door == null || _playerHands == null
                    || !_gameManager.IsPlaying || !_door.IsUnlocked || !_door.IsFullyOpen
                    || Time.timeScale <= 0f)
                return false;

            // se tiene en cuenta la posibilidad de que el jugador salve a dos niños a la vez
            foreach (ChildCompanion child in _children)
            {
                if (child != null && _playerHands.IsAccompanying(child))
                    return true;
            }

            return false;
        }
    }
    #endregion

    #region Unity
    private void Awake()
    {
        // se guardan las referencias
        GetComponent<BoxCollider>().isTrigger = true;

        Rigidbody body = GetComponent<Rigidbody>();
        // y se configura el rigidbody para evitar que la puerta "se caiga"
        body.isKinematic = true;
        body.useGravity = false;
    }

    private void Update()
    {
        // para mostrar el feedback al usuario de que ha salvado a los niños
        if (ActiveZone == this && _feedback != null)
            _feedback.SetRescuePrompt(CanRescue);
    }

    private void OnDisable()
    {
        _playerInside = false;

        if (ActiveZone == this)
            ActiveZone = null;

        if (_feedback != null)
            _feedback.SetRescuePrompt(false);
    }
    #endregion

    #region Colliders
    // Comprueba si el jugador entra en la zona de rescate
    private void OnTriggerEnter(Collider other)
    {
        RegisterPlayer(other);
    }

    // Comprueba si el jugador se mantiene dentro de la zona de rescate
    private void OnTriggerStay(Collider other)
    {
        RegisterPlayer(other);
    }

    // Comprueba si el jugador sale de la zona de rescate
    private void OnTriggerExit(Collider other)
    {
        if (other != _playerCollider)
            return;

        _playerInside = false;

        if (ActiveZone == this)
            ActiveZone = null;

        if (_feedback != null)
            _feedback.SetRescuePrompt(false);
    }
    #endregion

    // Indica que el jugador está en la zona de rescate
    private void RegisterPlayer(Collider other)
    {
        if (other != _playerCollider)
            return;

        _playerInside = true;
        ActiveZone = this;
    }

    // Intenta rescatar a un niño
    public void TryRescueChildren()
    {
        if (!CanRescue)
            return;

        int savedNow = 0;   // contador de niños rescates
        // se contabiliza cada niño que lleve de la manos (dos máximo)
        foreach (ChildCompanion child in _children)
        {
            if (child != null && _playerHands.IsAccompanying(child)
                && _gameManager.TryRescueChild(child))
            {
                savedNow++;
            }
        }

        // si no lleva ningún niño de la mano, sale
        if (savedNow == 0)
            return;

        // se muestra el feedback de rescate al jugador
        if (_feedback != null)
        {
            _feedback.SetRescuePrompt(false);
            _feedback.ShowRescueProgress(
                savedNow,
                _gameManager.RescuedChildrenCount,
                _gameManager.RequiredChildren);
        }
    }
}
