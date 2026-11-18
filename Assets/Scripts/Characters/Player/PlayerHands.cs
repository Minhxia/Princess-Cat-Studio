using UnityEngine;

[DisallowMultipleComponent]

// Clase que gestiona los objetos en las manos del jugador (dos como máximo, uno en cada mano).
// También gestiona la acción de darle la mano a los niños.
public class PlayerHands : MonoBehaviour
{
    #region Variables
    [Header("Hand grips")]
    [SerializeField] private Transform _leftHandGrip;
    [SerializeField] private Transform _rightHandGrip;

    // para poder comenzar con un objeto equipado desde el inicio (en principio, alguna luz)
    [Header("Starting item")]
    [SerializeField] private GameObject _startingItem;

    // este campo no hay que rellenarlo en el Inspector, es solo info para debug
    [Header("Held items (runtime, do not fill)")]
    [SerializeField] private GameObject _leftItem;
    [SerializeField] private GameObject _rightItem;

    private GameObject _leftUsedItem;
    private GameObject _rightUsedItem;

    private ChildCompanion _leftChild;
    private ChildCompanion _rightChild;
    #endregion

    #region Getters&Properties
    // OBJETOS
    public GameObject LeftItem => _leftItem;
    public GameObject RightItem => _rightItem;

    // NIÑOS
    public bool LeftHandHasChild => _leftChild != null;
    public bool RightHandHasChild => _rightChild != null;
    public bool IsHoldingChild => LeftHandHasChild || RightHandHasChild;
    // VELOCIDAD DEL JUGADOR
    // Layla ralentiza al jugador
    public float CompanionWalkSpeedMultiplier
    {
        get
        {
            float leftMultiplier = LeftHandHasChild ? _leftChild.WalkSpeedMultiplier : 1f;
            float rightMultiplier = RightHandHasChild ? _rightChild.WalkSpeedMultiplier : 1f;

            return Mathf.Min(leftMultiplier, rightMultiplier);
        }
    }
    // Layla no permite al jugador correr
    public bool CompanionBlocksRunning =>
        (LeftHandHasChild && _leftChild.BlocksRunning) ||
        (RightHandHasChild && _rightChild.BlocksRunning);

    // MANOS LIBRES
    private bool IsLeftHandFree => _leftItem == null && !LeftHandHasChild;
    private bool IsRightHandFree => _rightItem == null && !RightHandHasChild;
    public bool HasFreeHand => IsLeftHandFree || IsRightHandFree;
    public bool BothHandsHaveChildren => LeftHandHasChild && RightHandHasChild;
    public bool PickupUsesLeftHand => IsLeftHandFree || RightHandHasChild;
    #endregion

    #region Unity
    private void Start()
    {
        // si no se equipa nada desde el incio, sale
        if (_startingItem == null) return;

        if (!TryHold(_startingItem))
        {
            Debug.LogWarning("[PlayerHands] No se ha podido equipar el objeto inicial: "
                + _startingItem.name, this);
        }
    }

    private void OnDisable()
    {
        if (_leftChild != null)
            _leftChild.ReleaseHand();

        if (_rightChild != null)
            _rightChild.ReleaseHand();

        EndUseLeft();
        EndUseRight();
    }
    #endregion

    #region Hold
    // Intenta sostener en una mano un objeto o la mano de un niño al alcance del jugador
    public bool TryHold(GameObject item)
    {
        // si no se recibe ningún objeto o ese objeto ya está en una de las manos
        if (item == null || item == _leftItem || item == _rightItem) return false;

        // ya tiene las manos ocupadas por dos niños (los niños NO se sueltan automáticamente)
        if (BothHandsHaveChildren)
            return false;

        // la mano izquierda tiene prioridad, siempre que esté libre, se usará
        // (aunque la derecha también esté libre)
        bool useLeftHand = PickupUsesLeftHand;

        // en caso de que ambas manos estén ocupadas
        //if (!useLeftHand && _rightItem != null) return false;
        // AHORA se permite coger otro objeto aunque estén las dos manos ocupadas!

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

        // si las dos manos están ocupadas, NORMALMENTE se suelta el objeto de la derecha,
        // a no ser que esta mano lleve a un niño
        if (!HasFreeHand)
        {
            bool droppedItem = useLeftHand ? TryDrop(ref _leftItem) : TryDrop(ref _rightItem);

            if (!droppedItem)
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
    #endregion

    #region Drop
    // Intenta soltar un objeto
    private bool TryDrop(ref GameObject heldItem)
    {
        if (heldItem == null)
        {
            Debug.Log("[PlayerHands] No hay ningún objeto para soltar.");
            return false;
        }

        // al soltar el objeto, solo se conserva la posición del padre
        heldItem.transform.SetParent(null, true);

        // el resto de la configuración de las físicas se restablece
        Rigidbody body = heldItem.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.isKinematic = false;
            body.useGravity = true;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
        foreach (Collider collider in heldItem.GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = true;
        }
        if (body != null)
        {
            body.WakeUp();
        }

        Debug.Log("[PlayerHands] Ha soltado el objeto: " + heldItem.name);
        heldItem = null;
        return true;
    }

    // Intenta soltar el objeto o niño de la mano izquierda
    public bool TryDropLeft()
    {
        // niño de la mano izda
        if (_leftChild != null)
        {
            _leftChild.ReleaseHand();
            return true;
        }

        // objeto
        return TryDrop(ref _leftItem);
    }

    // Intenta soltar el objeto o niño de la mano derecha
    public bool TryDropRight()
    {
        // niño de la mano dcha
        if (_rightChild != null)
        {
            _rightChild.ReleaseHand();
            return true;
        }

        return TryDrop(ref _rightItem);
    }
    #endregion

    #region Transfer
    // Transfiere un objeto de una de las manos del jugador a otro punto del escenario 
    // (por ejemplo, a la mano de la estatua al ofrecerle una ofrenda)
    public bool TryTransferTo(GameObject item, Transform destination)
    {
        if (!isActiveAndEnabled || item == null || destination == null)
            return false;

        // si el punto de destino está "dentro" del propio jugador,
        // o del propio objeto 
        if (destination.IsChildOf(transform) || destination.IsChildOf(item.transform))
            return false;

        bool isLeftItem = item == _leftItem;
        bool isRightItem = item == _rightItem;

        // si no es ninguno de los objetos sostenidos
        if (!isLeftItem && !isRightItem)
            return false;

        Rigidbody body = item.GetComponent<Rigidbody>();
        if (body == null)
            return false;

        // se finaliza el uso y se libera la mano que sostenía el objeto
        // mano izquierda
        if (isLeftItem)
        {
            EndUseLeft();
            _leftItem = null;
        }
        // mano derecha
        else
        {
            EndUseRight();
            _rightItem = null;
        }

        // se transfiere el objeto a su nuevo destino
        body.isKinematic = true;
        body.useGravity = false;
        foreach (Collider collider in item.GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = false;
        }
        item.GetComponent<PickupOutline>().SetHighlighted(false);
        item.transform.SetParent(destination, true);
        item.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        return true;
    }
    #endregion

    #region Use 
    // Inicia el uso del objeto
    private void BeginUse(GameObject heldItem, GameObject otherHandItem, ref GameObject usedItem)
    {
        // si ya se está usando un objeto o si la mano está vacía, sale
        if (usedItem != null || heldItem == null) return;

        // objeto que se usa sobre otro objeto
        IHandConsumableWithOtherHand consumable = heldItem.GetComponent<IHandConsumableWithOtherHand>();
        if (consumable != null)
        {
            if (!consumable.TryUse(otherHandItem)) return;

            // se libera la mano al usar el objeto
            if (_leftItem == heldItem)
            {
                _leftItem = null;
            }
            else if (_rightItem == heldItem)
            {
                _rightItem = null;
            }
            // y se destruye
            Debug.Log("[PlayerHands] Has usado: " + heldItem.name + " en: " + otherHandItem.name);
            heldItem.SetActive(false);
            Destroy(heldItem);

            return;
        }

        IHandUsable usable = heldItem.GetComponent<IHandUsable>();
        if (usable == null) return;

        // se inicia el uso del objeto
        usedItem = heldItem;
        usable.OnUsePressed();
    }

    // Termina el uso del objeto
    private void EndUse(ref GameObject usedItem)
    {
        GameObject item = usedItem;
        usedItem = null;
        if (item != null)
        {
            item.GetComponent<IHandUsable>()?.OnUseReleased();
        }
    }

    // Inicia el uso del objeto de la mano izquierda
    public void BeginUseLeft()
    {
        BeginUse(_leftItem, _rightItem, ref _leftUsedItem);
    }

    // Termina el uso del objeto de la mano izquierda
    public void EndUseLeft()
    {
        EndUse(ref _leftUsedItem);
    }

    // Inicia el uso del objeto de la mano derecha
    public void BeginUseRight()
    {
        BeginUse(_rightItem, _leftItem, ref _rightUsedItem);
    }

    // Termina el uso del objeto de la mano derecha
    public void EndUseRight()
    {
        EndUse(ref _rightUsedItem);
    }
    #endregion

    #region Children
    // Indica si ya lleva de la mano a uno de los niños
    public bool IsAccompanying(ChildCompanion child)
    {
        return child != null && (_leftChild == child || _rightChild == child);
    }

    // Comprueba si lleva al niño de la mano izquierda (ya que ésta siempre tiene preferencia)
    public bool IsChildInLeftHand(ChildCompanion child)
    {
        return child != null && _leftChild == child;
    }

    // Comprueba si hay una mano libre para coger de la mano al niño, si está sujetando un objeto
    // en una mano y a un niño en la otra, da la opción de tirar el objeto para coger la mano del
    // nuevo niño
    public bool TryReserveChildHand(ChildCompanion child)
    {
        // se comprueba si ya lleva al niño dado de la mano o lleva a un niño de cada mano (dos a la vez)
        if (!isActiveAndEnabled || child == null || IsAccompanying(child) || BothHandsHaveChildren)
            return false;

        // se comprueban las manos
        bool useLeftHand = PickupUsesLeftHand;

        // si no tiene una mano libre, comprueba si en alguna lleva un objeto recogible que
        // pueda soltar
        if (!HasFreeHand)
        {
            bool droppedItem = useLeftHand ? TryDrop(ref _leftItem) : TryDrop(ref _rightItem);

            if (!droppedItem)
                return false;
        }

        if (useLeftHand) {
            _leftChild = child;
            EndUseLeft();
        }
        else {
            _rightChild = child;
            EndUseRight();
        }

        return true;
    }

    // Limpia la referencia del niño
    public void ClearChildReservation(ChildCompanion child)
    {
        if (_leftChild == child)
            _leftChild = null;
        else if (_rightChild == child)
            _rightChild = null;
    }
    #endregion
}
