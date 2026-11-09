using UnityEngine;

// Datos compartidos por un tipo de objeto
[CreateAssetMenu (
    fileName = "NewItemDefinition",
    menuName = "Items/Item Definition"
)]

// Clase que representa/define un tipo de objeto
public class ItemDefinition : ScriptableObject
{
    // de momento solo tiene nombre
    // TODO: más adelante añadir icono, descripción, etc.
    [SerializeField] private string _displayName;

    public string DisplayName => _displayName;
}
