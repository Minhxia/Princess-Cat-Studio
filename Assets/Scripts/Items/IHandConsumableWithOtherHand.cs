using UnityEngine;

// Interfaz para cualquier objeto que se consuma solo si ha podido aplicarse al
// objeto de la otra mano (por ejemplo: pilas + linterna)
public interface IHandConsumableWithOtherHand
{
    bool TryUse (GameObject otherHandItem);
}
