using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]

// Clase que representa un objeto con una interacción distinta de la de recogida (por ejemplo,
// un puzzle)
public class InteractableItem : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private bool _interactionEnabled = true;
    [SerializeField] private UnityEvent _onInteract = new UnityEvent();

    [Header("Visual feedback")]
    [SerializeField] private PickupOutline _interactionOutline;
    [SerializeField] private string _interactionPrompt = "[F] Interact";

    public bool CanInteract => isActiveAndEnabled && _interactionEnabled;
    public PickupOutline InteractionOutline => _interactionOutline;

    public string InteractionPrompt => string.IsNullOrWhiteSpace(_interactionPrompt)
        ? "[F] Interact" : _interactionPrompt;
        
    // Solicita la acción configurada (la del puzzle)
    public bool TryInteract()
    {
        if (!CanInteract)
            return false;
        
        _onInteract?.Invoke();
        return true;
    }

    // Setter, para llamarlo desde otros scripts (el del puzzle por ejemplo)
    public void SetInteractionEnabled(bool interactionEnabled)
    {
        _interactionEnabled = interactionEnabled;
    }
}
