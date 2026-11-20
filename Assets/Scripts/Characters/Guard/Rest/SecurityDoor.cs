using UnityEngine;
using UnityEngine.AI;

// Clase que representa la puerta de la garita del guardia.
public class SecurityDoor : MonoBehaviour
{
    #region Variables
    [SerializeField] private Transform _doorLeaf;   // parte de la puerta que se mueve al abrirse
    //[SerializeField, Range(-180f, 180f)] private float _openAngle = 90f; // giro para abrirse
    [SerializeField] private Vector3 _openOffset = new Vector3(1.2f, 0f, 0f); // deslizamiento para abrirse
    [SerializeField, Min(0.01f)] private float _travelSeconds = 0.8f;  // tiempo para abrirse o cerrarse
    [SerializeField, Min(0f)] private float _openSeconds = 3f; // tiempo que permanece abierta
    [SerializeField] private NavMeshObstacle _closedObstacle;  // obstáculo que impide cruzar la puerta
    [SerializeField] private BoxCollider _passageVolume;    // collider para la zona de paso
    [SerializeField] private LayerMask _characterLayers;    // capa de los personajes
    
    //private Quaternion _closedRotation;    // posición cerrada de la puerta
    private Vector3 _closedPosition;
    private float _progress;    // apertura actual
    private float _openUntil;   // momento hasta el que debe mantenerse abierta
    private bool _heldByGuard;  // indica si el guardia está pidiendo mantenerla abierta
    #endregion

    // Indica si la puerta se ha abierto del todo
    public bool IsFullyOpen => _progress >= 1f;

    #region Unity
    private void Awake()
    {
        if (_doorLeaf == null || _passageVolume == null || _closedObstacle == null)
        {
            Debug.LogError("[SecurityDoor] Asigna los valores correspondientes en el Inspector.", this);
            enabled = false;
            return;
        }

        // se guarda la posición inicial de la puerta como posición cerrada
        //_closedRotation = _doorLeaf.localRotation;
        _closedPosition = _doorLeaf.localPosition;
        // se activa la puerta como un obstáculo en el NavMesh
        _closedObstacle.carving = true;
        _closedObstacle.enabled = true;
    }

    // Actualiza la apertura de la puerta
    private void Update()
    {
        // indica si hay que mantener la puerta abierta
        bool keepOpen = _heldByGuard || Time.time < _openUntil;
        // en caso de que la puerta se estuviera abriendo o cerrando y justo esté pasando
        // algún personaje, se mantendrá abierta o volverá a abrirse
        if (_progress > 0f && PassageOccupied())
            keepOpen = true;

        // se establece la apertura de la puerta
        float target = 0f;
        if (keepOpen)
            target = 1f;

        // se actualiza el progreso de ese giro para abrir o cerrar la puerta
        _progress = Mathf.MoveTowards(_progress, target, Time.deltaTime / Mathf.Max(0.01f, _travelSeconds));
        // se gira la puerta
        //_doorLeaf.localRotation = _closedRotation * Quaternion.Euler(0f, _openAngle * _progress, 0f);
        // se desliza la puerta
        _doorLeaf.localPosition = _closedPosition + _openOffset * _progress;
        // y se actuliza el obstáculo de la puerta, cuando esté completamente abierta se desactiva
        _closedObstacle.enabled = !IsFullyOpen;
    }
    #endregion

    #region Door
    // Indica el momento hasta el que debe mantenerse abierta la puerta
    public void Open()
    {
        // en el caso de que se produzca una nueva solicitud de apertura,
        // se elige siempre el mayor tiempo
        // se suman: momento en que se pide + tiempo que permanece abierta + tiempo que tarda en abrirse/cerrarse
        _openUntil = Mathf.Max(_openUntil, Time.time + _openSeconds + _travelSeconds);
    }

    // El guardia mantiene el paso abierto mientras cruza.
    public void HoldOpenForGuard()
    {
        _heldByGuard = true;
        Open();
    }

    public void ReleaseGuardPassage()
    {
        _heldByGuard = false;
        Open();
    }

    // Conectar al botón de salida situado dentro de la sala 13.
    public void OpenFromInside()
    {
        Open();
    }

    private bool PassageOccupied()
    {
        Transform volume = _passageVolume.transform;
        Vector3 scale = volume.lossyScale;
        scale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        Vector3 halfSize = Vector3.Scale(_passageVolume.size * 0.5f, scale);
        return Physics.CheckBox(volume.TransformPoint(_passageVolume.center), halfSize,
            volume.rotation, _characterLayers, QueryTriggerInteraction.Ignore);
    }
    #endregion
}
