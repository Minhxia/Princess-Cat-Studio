using UnityEngine;

// Clase que gestiona los objetos en las manos del jugador
public class PlayerHands : MonoBehaviour
{
    #region Variables
    [Header("Hand grips")]
    [SerializeField] private Transform _leftHandGrip;
    [SerializeField] private Transform _rightHandGrip;

    // este campo no hay que rellenarlo en el Inspector, es solo info para debug
    [Header("Held items (runtime, do not fill)")]
    [SerializeField] private GameObject _leftItem;
    [SerializeField] private GameObject _rightItem;
    #endregion

    // Consulta si hay al menos una mano libre
    public bool HasFreeHand => _leftItem == null || _rightItem == null;

    public bool TryHold(GameObject item)
    {
        // si no se recibe ningún objeto o ese objeto ya está en una de las manos
        if (item == null || item == _leftItem || item == _rightItem) return false;

        // la mano izquierda tiene prioridad, siempre que esté libre, se usará
        // (aunque la derecha también esté libre)
        bool useLeftHand = _leftItem == null;

        // en caso de que ambas manos estén ocupadas
        if (!useLeftHand && _rightItem != null) return false;

        // se elige el punto de agarre
        Transform grip = useLeftHand ? _leftHandGrip : _rightHandGrip;
        if (grip == null)
        {
            Debug.LogWarning("[PlayerHands] Falta asignar un punto de agarre.", this);
            return false;
        }
        Rigidbody body = item.GetComponent<Rigidbody>();
        if (body == null)
        {
            Debug.LogWarning("[PlayerHands] El objeto necesita un Rigidbody: " + item.name, item);
            return false;
        }
        Collider[] colliders = item.GetComponentsInChildren<Collider>();
        if (colliders.Length == 0)
        {
            Debug.LogWarning("[PlayerHands] El objeto necesita un Collider: " + item.name, item);
            return false;
        }

        // se desactiva la física del objeto ya que se va a mover con la mano
        body.isKinematic = true;
        body.useGravity = false;
        // también se desactivan los colliders, para evitar choques con el jugador y
        // tapar el raycast de la mirada
        foreach (Collider collider in colliders)
        {
            collider.enabled = false;
        }

        // finalmente, el objeto se coloca en una mano
        item.transform.SetParent(grip, true);
        // cada objeto puede indicar cómo se coloca respecto a la mano (para evitar que
        // por ejemplo, la linterna gire al cogerla y apunte hacia arriba)
        ItemHoldPose3D holdPose = item.GetComponent<ItemHoldPose3D>();
        item.transform.SetLocalPositionAndRotation(
            holdPose != null ? holdPose.LocalPosition : Vector3.zero,
            holdPose != null ? holdPose.LocalRotation : Quaternion.identity);
        // se coloca en la mano izquierda
        if (useLeftHand)
        {
            _leftItem = item;
        }
        // o en la derecha
        else
        {
            _rightItem = item;
        }

        return true;
    }
}
