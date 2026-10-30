using UnityEngine;
using TMPro;

// Clase que gestiona la interacción del jugador con objetos recogibles
// o interactuables (de puzzles)
[RequireComponent(typeof(PlayerHands))]
public class PlayerItemInteraction : MonoBehaviour
{
    #region Variables
    [Header("Interaction settings")]
    [SerializeField] private Transform _firstPersonCamera;
    [SerializeField, Min(0.1f)] private float _pickupRadius = 2.5f;
    [SerializeField, Min(0f)] private float _pickupCenterHeight = 1f;
    [SerializeField, Range(0.01f, 0.9f)] private float _aimTolerance = 0.18f;

    [Header("Interaction layers")]
    [SerializeField] private LayerMask _pickupLayerMask;
    [SerializeField] private LayerMask _interactLayerMask;

    [Header("Interaction UI")]
    [SerializeField] private GameObject _pickupPromptPanel;
    [SerializeField] private TMP_Text _pickupPromptText;

    private PlayerHands _hands;

    private PickupOutline _highlightedOutline;
    #endregion

    #region Unity Methods
    private void Awake()
    {
        _hands = GetComponent<PlayerHands>();

        // hay que ocultar el mensaje de recogida al iniciar
        UpdatePickupPromptText(false);
    }

    private void LateUpdate()
    {
        PickupOutline nextOutline = null;
        bool hasTarget = false;     // indica si hay que mostrar el panel
        bool isInteraction = false; // indica qué mensaje debe aparecer en el texto

        // se muestra el contorno, siempre y cuando el juego no esté pausado
        if (Time.timeScale > 0f && _hands != null
            && TryFindInteractionItem(out GameObject item, out InteractableItem interactable))
        {
            hasTarget = true; // hay un objeto
            isInteraction = interactable != null;

            // el contorno solo se muestra en objetos recogibles
            if (item != null)
            {
                nextOutline = item.GetComponent<PickupOutline>();
                if (nextOutline != null && !nextOutline.isActiveAndEnabled)
                {
                    nextOutline = null;
                }
            }


        }

        SetHighlightedOutline(nextOutline);
        UpdatePickupPromptText(hasTarget, isInteraction);
    }

    // Apaga el contorno al desactivarse el componente
    private void OnDisable()
    {
        SetHighlightedOutline(null);
        UpdatePickupPromptText(false);
    }
    #endregion

    #region Hightlighted Methods
    // Muestra u oculta el contorno del objeto
    private void SetHighlightedOutline(PickupOutline nextOutline)
    {
        // si es el mismo objeto que ya se estaba mirando, no hace falta cambiar nada
        if (_highlightedOutline == nextOutline)
            return;

        // primero, se apaga el contorno del objeto anterior!
        if (_highlightedOutline != null)
            _highlightedOutline.SetHighlighted(false);

        // luego, se le da la referencia del nuevo objeto que se está mirando
        // para mostrar su contorno
        _highlightedOutline = nextOutline;

        if (_highlightedOutline != null)
        {
            _highlightedOutline.SetHighlighted(true);
        }
    }

    // Muestra u oculta el aviso de recogida o interacción
    private void UpdatePickupPromptText(bool visible, bool isInteraction = false)
    {
        if (_pickupPromptPanel == null || _pickupPromptText == null) return;

        visible = visible && _hands != null;

        // si ya se está mostrando, se oculta
        if (visible)
        {
            // en función de si es un objeto recogible o interactuable, se cambia el mensaje
            string message;
            // interactuable
            if (isInteraction)
            {
                message = "[F] Interact";
            }
            // recogible
            else
            {
                message = _hands.HasFreeHand
                    ? "[F] Pick up" : "[F] Pick up\n<size=75%>Drops right-hand item</size>";
            }

            // hay que asegurarse que el mensaje que se va a mostrar coincide
            if (_pickupPromptText.text != message)
            {
                _pickupPromptText.text = message;
            }
        }

        // finalmente, se muestra u oculta el texto
        if (_pickupPromptPanel.activeSelf != visible)
            _pickupPromptPanel.SetActive(visible);

    }
    #endregion

    #region Interaction Methods
    // Intenta interactuar con un objeto al alcance del jugador
    public void TryInteract()
    {
        if (!isActiveAndEnabled || Time.timeScale <= 0f) return;

        if (_hands == null)
        {
            Debug.LogWarning("[PlayerItemInteraction] Falta PlayerHands en este objeto.", this);
            return;
        }

        /*if (!_hands.HasFreeHand)
        {
            Debug.Log("[PlayerItemInteraction] Las dos manos están ocupadas.");
            return;
        }*/
        // AHORA dejamos recoger objetos siempre, y se suelta siempre el de la mano derecha

        if (!TryFindInteractionItem(out GameObject item, out InteractableItem interactable))
        {
            Debug.Log("[PlayerItemInteraction] No se ha encontrado ningún objeto para recoger o interactuar.");
            return;
        }

        // si es un objeto interactuable, de puzzle, se solicita su interacción
        if (interactable != null)
        {
            if (interactable.TryInteract())
            {
                // se apaga el contorno del objeto y se oculta el texto
                SetHighlightedOutline(null);
                UpdatePickupPromptText(false);
            }

            return;
        }

        // si es un objeto recogible, se coge con la mano
        if (item != null && _hands.TryHold(item))
        {
            // se apaga el contorno del objeto y se oculta el texto
            SetHighlightedOutline(null);
            UpdatePickupPromptText(false);

            Debug.Log("[PlayerItemInteraction] Objeto recogido: " + item.name);
        }
    }

    // Intenta encontrar un objeto al alcance del jugador y si lo hay, lo devuelve
    // (este tiene que estar dentro del área de recogida y el jugador tiene que estar mirando hacia él)
    /*private bool TryFindPickupItem(out GameObject item)
    {
        item = null;

        if (_firstPersonCamera == null)
        {
            Debug.LogWarning("[PlayerItemInteraction] No se ha asignado la cámara en primera persona!");
            return false;
        }

        // se buscan los objetos dentro del área de recogida del jugador
        // (si hay una pared delante del objeto, no se detecta, es decir, no se puede
        // coger un objeto atravesando la pared)
        Vector3 center = transform.position + Vector3.up * _pickupCenterHeight;
        Collider[] nearby = Physics.OverlapSphere(
            center,
            _pickupRadius,
            _pickupLayerMask,
            QueryTriggerInteraction.Ignore
        );

        // se realiza un raycast desde la cámara para determinar qué objeto está mirando
        // (primero el que está justo en el centro de la mirada)
        float maxRayDistance = _pickupRadius + Vector3.Distance(_firstPersonCamera.position, center);
        if (Physics.Raycast(
            _firstPersonCamera.position,
            _firstPersonCamera.forward,
            out RaycastHit rayHit,
            maxRayDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore)
            && TryGetNearbyItem(rayHit.collider, nearby, out item))
        {
            //Debug.Log("[PlayerItemInteraction] Hay un objeto al alcance del jugador con RayCast.");
            return true;
        }

        // en caso de que el raycast (un rayo finao) no haya seleccionado nada, se realiza
        // un spherecast (un rayo más grueso) por si acaso el objeto no está justo en el centro
        // de la mirada
        if (Physics.SphereCast(
            _firstPersonCamera.position,
            _aimTolerance,
            _firstPersonCamera.forward,
            out RaycastHit sphereHit,
            maxRayDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore)
            && TryGetNearbyItem(sphereHit.collider, nearby, out item))
        {
            //Debug.Log("[PlayerItemInteraction] Hay un objeto al alcance del jugador con SphereCast.");
            return true;
        }

        //Debug.Log("[PlayerItemInteraction] No hay ningún objeto al alcance del jugador.");
        return false;
    }*/

    // Intenta encontrar un objeto recogible o interactuable (éste tiene que estar 
    // dentro del área de recogida y el jugador tiene que estar mirando hacia él)
    private bool TryFindInteractionItem(out GameObject item, out InteractableItem interactable)
    {
        item = null;
        interactable = null;

        if (_firstPersonCamera == null)
        {
            Debug.LogWarning("[PlayerItemInteraction] No se ha asignado la cámara en primera persona!");
            return false;
        }

        // se buscan los objetos dentro del área de recogida del jugador
        // (si hay una pared delante del objeto, no se detecta, es decir, no se puede
        // coger un objeto atravesando la pared)
        Vector3 center = transform.position + Vector3.up * _pickupCenterHeight;
        // se buscan los objetos en las 2 capas
        int interactionLayers = _pickupLayerMask | _interactLayerMask;
        Collider[] nearby = Physics.OverlapSphere(
            center,
            _pickupRadius,
            interactionLayers,
            QueryTriggerInteraction.Ignore
        );

        if (nearby.Length == 0)
        {
            //Debug.Log("[PlayerItemInteraction] No hay objetos cercanos. Nearby Length = " + nearby.Length);
            return false;
        }

        // se realiza un raycast desde la cámara para determinar qué objeto está mirando
        float maxRayDistance = _pickupRadius + Vector3.Distance(_firstPersonCamera.position, center);
        // primero el que está justo en el centro de la mirada
        if (Physics.Raycast(
            _firstPersonCamera.position,
            _firstPersonCamera.forward,
            out RaycastHit rayHit,
            maxRayDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore)
            && TryGetNearbyItem(rayHit.collider, nearby, out item, out interactable))
        {
            //Debug.Log("[PlayerItemInteraction] Hay un objeto al alcance del jugador con RayCast.");
            return true;
        }

        // en caso de que el raycast (un rayo finao) no haya seleccionado nada, se realiza
        // un spherecast (un rayo más grueso) por si acaso el objeto no está justo en el centro
        // de la mirada
        if (Physics.SphereCast(
            _firstPersonCamera.position,
            _aimTolerance,
            _firstPersonCamera.forward,
            out RaycastHit sphereHit,
            maxRayDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore)
            && TryGetNearbyItem(sphereHit.collider, nearby, out item, out interactable))
        {
            //Debug.Log("[PlayerItemInteraction] Hay un objeto al alcance del jugador con SphereCast.");
            return true;
        }

        //Debug.Log("[PlayerItemInteraction] No hay ningún objeto al alcance del jugador.");
        return false;
    }

    // Intenta encontrar un objeto al alcance del jugador que esté siendo mirado
    // (para evitar recoger uno que tenga detrás por ejemplo), y determina si es
    // recogible o interactuable (puzzle)
    private bool TryGetNearbyItem(Collider hitCollider, Collider[] nearby,
                    out GameObject item, out InteractableItem interactable)
    {
        item = null;
        interactable = null;

        // se ignoran los objetos que el jugador ya lleva consigo
        if (hitCollider == null || hitCollider.transform.IsChildOf(transform))
        {
            return false;
        }

        // se comprueba si el objeto al que mira el jugador está dentro del área de recogida
        foreach (Collider collider in nearby)
        {
            // si no se mira, se ignora el objeto
            if (collider != hitCollider)
            {
                continue;
            }

            // se obtiene la capa del GameObject que contiene el collider
            int layerBit = 1 << collider.gameObject.layer;
            // primero se comprueba si pertenece a las capas de objetos interactuables
            if ((_interactLayerMask.value & layerBit) != 0)
            {
                InteractableItem candidate = collider.GetComponentInParent<InteractableItem>();
                if (candidate != null)
                {
                    // en caso de que no se pueda interactuar con el objeto, o ya lo lleve consigo
                    if (!candidate.CanInteract || candidate.transform.IsChildOf(transform))
                    {
                        return false;
                    }

                    interactable = candidate;
                    return true;
                }
            }

            // después, se comprueba si pertenece a las capas de objetos recogibles
            if ((_pickupLayerMask.value & layerBit) != 0)
            {
                Rigidbody body = collider.attachedRigidbody;
                if (body != null)
                {
                    // si ya lleva el objeto consigo, sale
                    if (body.transform.IsChildOf(transform))
                    {
                        return false;
                    }

                    item = body.gameObject;
                    return true;
                }
            }

            return false;
        }

        return false;
    }
    #endregion

    #region Drop Methods
    // Intenta soltar el objeto de la mano izquierda
    public void TryDropLeft()
    {
        if (_hands != null)
        {
            _hands.TryDropLeft();
        }
    }

    // Intenta soltar el objeto de la mano derecha
    public void TryDropRight()
    {
        if (_hands != null)
        {
            _hands.TryDropRight();
        }
    }
    #endregion

    #region Debug Methods
    // Debug visual de la zona de recogida + mirada del jugador
    private void OnDrawGizmosSelected()
    {
        Vector3 center = transform.position + Vector3.up * _pickupCenterHeight;

        // área de recogida de objetos
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, _pickupRadius);

        if (_firstPersonCamera == null) return;

        // línea del raycast
        Vector3 origin = _firstPersonCamera.position;
        float maxRayDistance = _pickupRadius
            + Vector3.Distance(origin, center);
        Vector3 end = origin + _firstPersonCamera.forward * maxRayDistance;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(origin, end);

        // volumen aproximado del spherecast
        Vector3 right = _firstPersonCamera.right * _aimTolerance;
        Vector3 up = _firstPersonCamera.up * _aimTolerance;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(origin, _aimTolerance);
        Gizmos.DrawWireSphere(end, _aimTolerance);
        Gizmos.DrawLine(origin + right, end + right);
        Gizmos.DrawLine(origin - right, end - right);
        Gizmos.DrawLine(origin + up, end + up);
        Gizmos.DrawLine(origin - up, end - up);
    }
    #endregion
}
