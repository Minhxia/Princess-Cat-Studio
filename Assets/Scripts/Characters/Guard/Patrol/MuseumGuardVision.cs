using UnityEngine;

// Clase que se encarga de comprobar si el guardia puede ver al jugador.
public class MuseumGuardVision
{
    #region Variables
    private readonly Transform _guard;  // posición del guardia
    private readonly Transform _player; // posición del jugador
    private readonly Transform _target; // qué mira el guardia para ver al jugador
    private readonly Transform _origin; // posición desde donde sale la visión
    private readonly float _rangeSquared;   // alcance de la visión
    private readonly float _halfAngle;      // ángulo del cono de visión
    private readonly LayerMask _obstacles;  // obstáculos que tapan la visión al guardia
    private bool _reportedCurrentSighting;  // indica si se ha dado el aviso
    #endregion

    #region Getters
    public int SightingRoomID { get; private set; }
    public Vector3 SightingPosition { get; private set; }
    #endregion

    public MuseumGuardVision(Transform guard, Transform player, Transform target, Transform origin,
        float range, float angle, LayerMask obstacles)
    {
        _guard = guard;
        _player = player;
        _target = target;
        _origin = origin;
        _rangeSquared = range * range;
        _halfAngle = angle * 0.5f;
        _obstacles = obstacles;
    }

    // Comprueba si el guardia ve al jugador
    public bool DetectPlayer(int currentRoomID)
    {
        if (_player == null || Time.deltaTime <= 0f)
            return false;

        // se calcula desde dónde mira el guardia
        // posición del guardia
        Vector3 origin = _origin != null ? _origin.position : _guard.position + Vector3.up * 1.6f;
        // dirección hacia delante
        Vector3 forward = _origin != null ? _origin.forward : _guard.forward;
        // posición del jugador
        Vector3 target = _target != null ? _target.position : _player.position + Vector3.up * 1.1f;
        // y hacia qué punto
        Vector3 offset = target - origin;

        // para saber si el guardia ve al jugador, primero se comprueba si se encuentra a una
        // distancia dentro del alcance y si entra dentro del cono de visión
        bool visible = offset.sqrMagnitude <= _rangeSquared
                            && Vector3.Angle(forward, offset) <= _halfAngle;
        // si está dentro del campo de visión, se comprueba si hay algún obstáculo
        // delante del jugador
        if (visible && Physics.Linecast(origin, target, out RaycastHit hit, _obstacles,
                QueryTriggerInteraction.Ignore))
            visible = hit.transform.IsChildOf(_player);

        // se comprueba si hace falta mandar el aviso
        if (!visible)
            _reportedCurrentSighting = false;
        if (!visible || _reportedCurrentSighting)
            return false;

        // se guarda la información para el aviso a los monstruos
        SightingPosition = _player.position;
        SightingRoomID = currentRoomID;

        return true;
    }

    // Indica el guardia ha visto al jugador y se envía el aviso con la información
    public void MarkSightingReported()
    {
        _reportedCurrentSighting = true;
    }

    #region Debug
    // Para visualizar mejor qué ve el guardia
    public static void DrawVisionGizmo(
    Transform guard, Transform visionOrigin, float range, float angle)
    {
        Transform view = guard;
        Vector3 origin = guard.position + Vector3.up * 1.6f;

        if (visionOrigin != null)
        {
            view = visionOrigin;
            origin = visionOrigin.position;
        }

        float halfAngle = angle * 0.5f * Mathf.Deg2Rad;
        float radius = range * Mathf.Sin(halfAngle);
        Vector3 rimCenter = origin + view.forward * (range * Mathf.Cos(halfAngle));

        Color previousColor = Gizmos.color;
        Gizmos.color = Color.yellow;

        // línea central (en la dirección de la mirada)
        Gizmos.DrawLine(origin, origin + view.forward * range);

        // borde circular, con cuatro líneas que salen del origen
        const int segments = 24;
        Vector3 previousPoint = rimCenter + view.right * radius;

        for (int i = 1; i <= segments; i++)
        {
            float radians = i * (2f * Mathf.PI / segments);
            Vector3 offset = view.right * (Mathf.Cos(radians) * radius)
                           + view.up * (Mathf.Sin(radians) * radius);
            Vector3 point = rimCenter + offset;

            Gizmos.DrawLine(previousPoint, point);

            if (i % (segments / 4) == 0)
                Gizmos.DrawLine(origin, point);

            previousPoint = point;
        }

        Gizmos.color = previousColor;
    }
    #endregion
}
