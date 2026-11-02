using UnityEngine;
using UnityEngine.Events;
using TMPro;

[DisallowMultipleComponent]

[RequireComponent(typeof(InteractableItem))]

// Clase que controla la lógica del puzzle de la estatua (el trueque de una ofrenda a 
// cambio de la lámpara de aceite)
public class StatueTradePuzzle : MonoBehaviour
{
    // Mini-clase con la información de la posible ofrenda (solución al puzzle)
    [System.Serializable] private class OfferingOption
    {
        public ItemDefinition definition;       // qué objeto representa
        [TextArea(2, 4)] public string clue;    // qué pista tiene ese objeto
    }

    #region Variables
    [Header("References")]
    [SerializeField] private PlayerHands _hands;
    [SerializeField] private Transform _offeringPoint; // indica dónde se colocará la ofrenda
    [SerializeField] private Rigidbody _oilLampBody;
    [SerializeField] private Transform _oilLampReleasePoint;
    [SerializeField] private InteractableItem _plaqueInteractable;

    [Header("Layers")]
    [SerializeField] private string _interactionLayerName = "Interactable";
    [SerializeField] private string _pickupLayerName = "Pickupable";

    [Header("Offering selection")]
    [SerializeField] private bool _randomizeOffering = false; // indica si se elige aleatoriamente
    [SerializeField, Min(0)] private int _fixedOfferingIndex = 0; // o se usa una ofrenda en concreto
    [SerializeField] private OfferingOption[] _offerings =
    {
        new OfferingOption(),
        new OfferingOption(),
        new OfferingOption()
    };

    [Header("Hint lights events")]
    [SerializeField] private UnityEvent<bool> _onPlaqueHintChanged = new UnityEvent<bool>();
    [SerializeField] private UnityEvent<bool> _onLampHintChanged = new UnityEvent<bool>();

    [Header("Feedback events")]
    [SerializeField] private UnityEvent _onRejected = new UnityEvent(); // el jugador no lleva la ofrenda correcta
    [SerializeField] private UnityEvent _onSolved = new UnityEvent();   // se completa el trueque

    private InteractableItem _interactable;
    private Collider[] _oilLampColliders;
    private ItemDefinition _requiredItem;
    private int _interactionLayer;  // número de la capa de los objetos interactuables
    private int _pickupLayer;       // número de la capa de los objetos recogibles
    
    private bool _ready;    // el puzzle está preparado para funcionar (preparación inicial correcta)
    private bool _isSolved;
    private TMP_Text _clueText;
    private bool _hasExaminedClue;  // el jugador ha examinado la placa y ha leído la pista
    private string _selectedClue;
    #endregion

    // Getters
    public bool IsSolved => _isSolved;
    public bool IsReady => _ready;
    public bool HasExaminedClue => _hasExaminedClue;
    public string Clue => _selectedClue;
    public ItemDefinition RequiredItem => _requiredItem;

    #region Unity Methods
    // Realiza la preparación inicial del puzzle
    private void Awake()
    {
        _interactable = GetComponent<InteractableItem>();
        // a partir de los nombres de las capas, se obtienen sus índices (números)
        _interactionLayer = LayerMask.NameToLayer(_interactionLayerName);
        _pickupLayer = LayerMask.NameToLayer(_pickupLayerName);

        // se evita seguir adelante con el puzzle si faltan referencias!
        if (!ValidateConfiguration())
        {
            _interactable.SetInteractionEnabled(false);
            // si se ha asignado mal la placa.
            if (_plaqueInteractable != null && _plaqueInteractable != _interactable)
                _plaqueInteractable.SetInteractionEnabled(false);
            return;
        }

        // se selecciona un objeto de los tres dados como ofrenda válida
        int index = _randomizeOffering 
            ? Random.Range(0, _offerings.Length) : _fixedOfferingIndex;
        Debug.Log("[StatueTradePuzzle] Offering index = " + index
            + " (random: " + _randomizeOffering + ")", this);
        
        OfferingOption selected = _offerings[index];
        _requiredItem = selected.definition;
        _selectedClue = selected.clue;

        // se muestra el texto de la pista
        if (_clueText != null)
        {
            _clueText.text = selected.clue;
        }

        // se prepara la lámpara de aceite
        _oilLampBody.isKinematic = true;
        _oilLampBody.useGravity = false;
        _oilLampBody.gameObject.layer = _interactionLayer;
        foreach (Collider collider in _oilLampColliders)
        {
            collider.gameObject.layer = _interactionLayer;
            collider.enabled = true;
        }
        PickupOutline lampOutline = _oilLampBody.GetComponent<PickupOutline>();
        if (lampOutline != null) lampOutline.SetHighlighted(false);

        // primero se examina la placa, y después de leearla,
        // se activa la interacción con la lámpara
        _interactable.SetInteractionEnabled(false);
        _plaqueInteractable.SetInteractionEnabled(true);

        _ready = true; // preparación hecha!
    }

    private void Start()
    {
        // se configura el estado inicial de los destellos
        UpdateHintLights();
    }
    #endregion

    #region Interaction Methods
    // Notifica que el jugador ha examinado la placa y leído la pista (solo realiza el cambio
    // la primera vez)
    public void NotifyClueExamined()
    {
        if (!_ready || !isActiveAndEnabled || Time.timeScale <= 0f || _hasExaminedClue ||
                _isSolved)
            return;

        _hasExaminedClue = true;
        _interactable.SetInteractionEnabled(true);
        UpdateHintLights();
    }

    // Actualiza el estado de los destellos de la placa y la lámpara
    private void UpdateHintLights()
    {
        _onPlaqueHintChanged?.Invoke(_ready && !_hasExaminedClue && !_isSolved);
        _onLampHintChanged?.Invoke(_ready && _hasExaminedClue && !_isSolved);
    }

    // Responde cuando el jugador pulsa F para interactuar con la lámpara de aceite
    // (si ya ha examinado la placa)
    public void Interact()
    {
        if (!_ready || !_hasExaminedClue || _isSolved
            || !isActiveAndEnabled || Time.timeScale <= 0f) return;

        // se comprueba si en las manos se tiene la ofrenda
        // por prioridad, siempre se comprueba primero la mano izquierda
        GameObject offering = FindOffering(_hands.LeftItem);
        // y luego, la derecha
        if (offering == null)
        {
            offering = FindOffering(_hands.RightItem);
        }

        // si ninguna mano lleva la ofrenda
        if (offering == null)
        {
            Debug.Log("[StatueTradePuzzle] El jugador no lleva la ofrenda correcta.", this);
            _onRejected?.Invoke();
            return;
        }

        // antes de realizar el trueque, se comprueban de nuevo la lámpara y el punto donde
        // se colocará la ofrenda
        if (_oilLampBody == null || _offeringPoint == null)
        {
            Debug.LogError("[StatueTradePuzzle] Falta la lámpara o el punto de ofrenda.", this);
            return;
        }
        // después, se intenta hacer la transferencia de objetos
        if (!_hands.TryTransferTo(offering, _offeringPoint)) return;
        // y si la transferencia tiene éxito, se resuelve el puzzle
        _isSolved = true;
        _interactable.SetInteractionEnabled(false);
        ReleaseLamp();
        UpdateHintLights();
        _onSolved?.Invoke();
    }

    // Comprueba el objeto de una de las manos
    private GameObject FindOffering(GameObject item)
    {
        if (item == null) return null;

        ItemIdentity identity = item.GetComponent<ItemIdentity>();
        // se comprueba si la definición del objeto coincide con la de la ofrenda buscada
        return identity != null && identity.Definition == _requiredItem ? item : null;
    }

    // Libera la lámpara
    private void ReleaseLamp()
    {
        // se saca la lámpara fuera de la estatua
        _oilLampBody.transform.SetParent(null, true);
        // y se coloca en el punto de liberación
        if (_oilLampReleasePoint != null)
        {
            _oilLampBody.position = _oilLampReleasePoint.position;
            _oilLampBody.rotation = _oilLampReleasePoint.rotation;
        }

        // una vez se suelta, se vuelve un objeto recogible
        _oilLampBody.gameObject.layer = _pickupLayer;
        foreach(Collider collider in _oilLampColliders)
        {
            if (collider == null) continue;
            
            collider.gameObject.layer = _pickupLayer;
            collider.enabled = true;
        }
        _oilLampBody.isKinematic = false;
        _oilLampBody.useGravity = true;
        _oilLampBody.linearVelocity = Vector3.zero;
        _oilLampBody.angularVelocity = Vector3.zero;
        _oilLampBody.WakeUp(); // "despierta" del reposo físico
    }

    #endregion

    #region Configuration Methods
    // Comprueba que todos los datos necesarios para el puzzle son correctos
    private bool ValidateConfiguration()
    {
        // si hay referencias sin asignar
        if (_hands == null || _offeringPoint == null || _oilLampBody == null)
        {
            return ConfigurationError("[StatueTradePuzzle] Asigna Hands, Offering Point y Lamp Body.");
        }

        // si el script de interacciónde la placa no está asignado, o es el mismo que el de la estatua
        if (_plaqueInteractable == null || _plaqueInteractable == _interactable
            || !_plaqueInteractable.transform.IsChildOf(transform)
            || _plaqueInteractable.transform.IsChildOf(_oilLampBody.transform))
        {
            return ConfigurationError("[StatueTradePuzzle] Asigna el InteractableItem de la placa fuera de la lámpara.");
        }

        // si la lámpara de aceite no es hija de la estatua
        if (!_oilLampBody.transform.IsChildOf(transform)
            || _oilLampBody.transform == transform)
        {
            return ConfigurationError("[StatueTradePuzzle] La lámpara debe ser hija de la estatua.");
        }

        // si el punto para colocar la ofrenda no está en la estatua (se supone que la ofrenda ocupará
        // el lugar de la lámpara en la estatua)
        if (!_offeringPoint.IsChildOf(transform)
            || _offeringPoint.IsChildOf(_hands.transform)
            || _offeringPoint.IsChildOf(_oilLampBody.transform))
        {
            return ConfigurationError("[StatueTradePuzzle] Offering Point debe estar en la estatua.");
        }

        // si el punto desde que la estatua suelta la lámpara al realizar la ofrenda está dentro de la propia lámpara
        // (no es un punto del escenario)
        if (_oilLampReleasePoint != null
            && _oilLampReleasePoint.IsChildOf(_oilLampBody.transform))
        {
            return ConfigurationError("[StatueTradePuzzle] Lamp Release Point debe estar fuera de la lámpara.");
        }

        // si las capas de objetos recogibles e interactuales no existen, o son la misma
        if (_interactionLayer < 0 || _pickupLayer < 0 || _interactionLayer == _pickupLayer)
        {
            return ConfigurationError("[StatueTradePuzzle] Indica dos layers existentes y diferentes.");
        }

        // si no se puede elegir una ofrenda válida
        if (_offerings == null || _offerings.Length == 0
            || (!_randomizeOffering
                && (_fixedOfferingIndex < 0 || _fixedOfferingIndex >= _offerings.Length)))
        {
            return ConfigurationError("[StatueTradePuzzle] Configura las ofrendas y un índice fijo válido.");
        }

        // si resulta que las distintas ofrendas no tienen todas una definición o se repite alguna
        for (int i = 0; i < _offerings.Length; i++)
        {
            if (_offerings[i] == null || _offerings[i].definition == null)
            {
                return ConfigurationError("[StatueTradePuzzle] Cada opción de ofrenda necesita una definición.");
            }

            if (string.IsNullOrWhiteSpace(_offerings[i].clue))
            {
                return ConfigurationError("Cada opción de ofrenda necesita una pista para poder examinar la placa.");
            }

            for (int j = 0; j < i; j++)
            {
                if (_offerings[i].definition == _offerings[j].definition)
                {
                    return ConfigurationError("[StatueTradePuzzle] Las opciones de ofrenda deben tener definiciones distintas.");
                }
            }
        }

        // se comprueba que haya al menos un collider para la lámpara
        _oilLampColliders = _oilLampBody.GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in _oilLampColliders)
        {
            if (!collider.isTrigger && collider.gameObject.activeInHierarchy
                && collider.attachedRigidbody == _oilLampBody)
            {
                return true;
            }
        }

        return ConfigurationError("[StatueTradePuzzle] La lámpara necesita un collider sólido activo asociado a su Rigidbody.");
    }

    // Informa sobre un problema/error en la configuración inicial
    private bool ConfigurationError(string message)
    {
        Debug.LogError("[StatueTradePuzzle] " + message, this);
        return false;
    }
    #endregion
}
