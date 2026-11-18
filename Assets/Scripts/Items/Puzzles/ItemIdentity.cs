using UnityEngine;

[DisallowMultipleComponent]

// Clase que identifica qué tipo de objeto representa este GameObject (al que está asociado)
public class ItemIdentity : MonoBehaviour
{
    [SerializeField] private ItemDefinition _definition;

    public ItemDefinition Definition => _definition;
}
