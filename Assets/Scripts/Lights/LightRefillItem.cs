using UnityEngine;

[DisallowMultipleComponent]

// Clase para los objetos que recargan la duración de una fuente de luz
// (de momento, pilas para la linterna, aceite para la lámpara)
public class LightRefillItem : MonoBehaviour, IHandConsumableWithOtherHand
{
    [SerializeField] private LightDuration.RefillType _refillType = LightDuration.RefillType.None;
    [SerializeField, Min(0.1f)] private float _restoreSeconds = 15f;

    // Indica si se puede usar el objeto en el objeto de la otra mano
    public bool TryUse(GameObject otherHandItem)
    {
        if (otherHandItem == null) return false;

        LightDuration duration = otherHandItem.GetComponent<LightDuration>();
        return duration != null && duration.TryRestore(_refillType, _restoreSeconds);
    }
}
