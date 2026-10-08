using UnityEngine;

// Clase que gestiona la interacción del jugador con objetos recogibles
public class PlayerItemInteraction : MonoBehaviour
{
    #region Variables
    [Header("Interaction settings")]
    [SerializeField] private Transform _firstPersonCamera;
    [SerializeField, Min(0.1f)] private float _pickupRadius = 2.5f;
    [SerializeField, Min(0f)] private float _pickupCenterHeight = 1f;
    [SerializeField, Range(0.01f, 0.5f)] private float _aimTolerance = 0.18f;
    [SerializeField] private LayerMask _pickupLayerMask;
    #endregion

    #region Other Methods
    // Intentar recoger un objeto al alcance del jugador
    public void TryPickup()
    {
        if(TryFindPickupItem(out GameObject item))
        {
            Debug.Log("[PlayerItemInteraction] Objeto recogible al alcance: " + item.name);
            //TODO: pasar el item a una mano
        }
    }

    // Intenta encontrar un objeto al alcance del jugador y si lo hay, lo devuelve
    // (este tiene que estar dentro del área de recogida y el jugador tiene que estar mirando hacia él)
    private bool TryFindPickupItem(out GameObject item)
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
            Debug.Log("[PlayerItemInteraction] Hay un objeto al alcance del jugador con RayCast.");
            return true;
        }

        // en caso de que el raycast (un rayo finao) no haya seleccionado nada, se realiza
        // un spherecast (un rayo más grueso) por si acaso el objeto no está justo en el centro
        // de la mirada
        if (Physics.SphereCast (
            _firstPersonCamera.position,
            _aimTolerance,
            _firstPersonCamera.forward,
            out RaycastHit sphereHit,
            maxRayDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore)
            && TryGetNearbyItem(sphereHit.collider, nearby, out item))
        {
            Debug.Log("[PlayerItemInteraction] Hay un objeto al alcance del jugador con SphereCast.");
            return true;
        }

        Debug.Log("[PlayerItemInteraction] No hay ningún objeto al alcance del jugador.");
        return false;
    }

    // Intenta encontrar un objeto al alcance del jugador que esté siendo mirado
    // (para evitar recoger uno que tenga detrás por ejemplo)
    private bool TryGetNearbyItem(Collider hitCollider, Collider[] nearby, out GameObject item)
    {
        item = null;

        // se comprueba si el objeto al que mira el jugador está dentro del área de recogida
        foreach (Collider collider in nearby)
        {
            // si no se mira, se ignora el objeto
            if (collider != hitCollider)
            {
                continue;
            }

            // se coge el objeto que tiene el collider,
            // o si tiene un rigidbody, se coge el objeto del rigidbody
            GameObject candidate = collider.attachedRigidbody != null ?
                collider.attachedRigidbody.gameObject : collider.gameObject;
            
            // si el objeto es "parte del jugador", se ignora, es decir, si lo lleva en una mano
            if (candidate.transform.IsChildOf(transform))
            {
                Debug.Log("[PlayerItemInteraction] El objeto al alcance del jugador es parte del jugador: " + candidate.name);
                return false;
            }

            item = candidate;
            return true;
        }
        return false;
    }

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
