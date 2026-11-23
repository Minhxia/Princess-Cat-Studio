using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(InteractableItem))]

// Clase que representa la tarjeta de seguridad.
public class SecurityCard : MonoBehaviour
{
    [SerializeField] private Transform _guard;          // guardia
    [SerializeField] private PlayerHands _playerHands;  // manos del jugador
    [SerializeField, Min(0.1f)] private float _takeDistance = 2f;  // distancia máxima para poder robar la tarjeta
    [SerializeField, Range(0f, 1f)] private float _backThreshold = 0.2f; // cómo de detrás tiene que estar el jugador
    [SerializeField, Min(0.1f)] private float _useSeconds = 1.2f; // tiempo que la usa el guardia

    private bool _isInUse;      // indica si el guardia la está usando
    private float _useEndsAt;   // momento en que termina de usarla

    // referencias:
    private Renderer[] _renderers;
    private Collider[] _colliders;

    private bool[] _rendererStates;
    private bool[] _colliderStates;

    public bool IsInUse => _isInUse;

    #region Unity
    private void Awake()
    {
        // se guardan las referencias de la tarjeta
        _renderers = GetComponentsInChildren<Renderer>(true);
        _colliders = GetComponentsInChildren<Collider>(true);

        _rendererStates = new bool[_renderers.Length];
        _colliderStates = new bool[_colliders.Length];
    }

    private void Update()
    {
        if (_isInUse && Time.time >= _useEndsAt)
            EndUse();
    }

    private void OnDisable()
    {
        // Restaura sus componentes si se desactiva durante el uso.
        EndUse();
    }
    #endregion

    #region Guard
    // Comienza el uso de la tarjeta por parte del guardia
    public void BeginUse()
    {
        if (_isInUse || !IsCarriedBy(_guard))
            return;

        // se indica que se está usando y se calcula cuándo termina
        _isInUse = true;
        _useEndsAt = Time.time + _useSeconds;

        // se guarda qué componentes están activados
        for (int i = 0; i < _renderers.Length; i++)
        {
            _rendererStates[i] = _renderers[i].enabled;
            // se oculta la tarjeta
            _renderers[i].enabled = false;
        }
        for (int i = 0; i < _colliders.Length; i++)
        {
            _colliderStates[i] = _colliders[i].enabled;
            // se bloquea su interacción (el jugador no la puede coger)
            _colliders[i].enabled = false;
        }
    }

    // Termina el uso de la tarjeta por parte del guardia
    public void EndUse()
    {
        if (!_isInUse)
            return;

        // se reestablecen los componentes
        //  de manera que la tarjeta vuelve a estar visible
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null)
                _renderers[i].enabled = _rendererStates[i];
        }
        // y el jugador ya puede volver a interactuar con ella (robarla)
        for (int i = 0; i < _colliders.Length; i++)
        {
            if (_colliders[i] != null)
                _colliders[i].enabled = _colliderStates[i];
        }

        _isInUse = false;
    }
    #endregion

    // Comprueba quién tiene la tarjeta
    public bool IsCarriedBy(Transform carrier)
    {
        // si no hay un personaje al que comprobar
        if (carrier == null)
            return false;

        // si la tarjeta está desactivada, no se considera disponible
        if (!gameObject.activeInHierarchy)
            return false;

        // comprueba si la tarjeta pertenece a ese personaje
        return transform.IsChildOf(carrier);
    }

    // Comprueba si se puede coger la tarjeta
    public void TakeCard()
    {
        // si el guardia la está usando, no se puede robar
        if (_isInUse)
            return;

        if (_playerHands == null || !_playerHands.isActiveAndEnabled || Time.timeScale <= 0f)
            return;

        // primero se comprueba la distancia hasta la tarjeta
        Vector3 playerPosition = _playerHands.transform.position;
        // si esa distancia supera el máximo, directamente se sale
        if (Vector3.Distance(playerPosition, transform.position) > _takeDistance)
            return;

        // si es el guardia quién lleva la tarjeta
        if (IsCarriedBy(_guard))
        {
            // dirección desde el guardia hacia el jugador
            Vector3 toPlayer = Vector3.ProjectOnPlane(playerPosition - _guard.position, Vector3.up);
            // dirección frontal del guardia
            Vector3 guardForward = Vector3.ProjectOnPlane(_guard.forward, Vector3.up);
            // se comprueba cómo de detrás está el jugador del guardia en función del resultado:
            // 1 -> está delante de él, 0 -> está a un lado, -1 -> está justo detrás
            if (Vector3.Dot(guardForward.normalized, toPlayer.normalized) > -_backThreshold)
                return;
        }

        // si todo es correcto, se le permite al jugador intentar coger la tarjeta
        if (_playerHands.TryHold(gameObject))
            Debug.Log("[SecurityCard] Tarjeta robada!", this);
    }
}
