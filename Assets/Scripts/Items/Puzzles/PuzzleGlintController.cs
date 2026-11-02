using UnityEngine;

[DisallowMultipleComponent]

[RequireComponent(typeof(ParticleSystem))]

// Clase que controla debe mostrarse el destello de un elemento/objeto de un puzzle
public class PuzzleGlintController : MonoBehaviour
{
    #region Variables
    [Header("Visibility settings")]
    [SerializeField] private Camera _playerCamera;
    [SerializeField, Min(0.1f)] private float _maxDistance = 5f; // distancia entre la cámara y el destello
    [SerializeField] private LayerMask _osbtacleLayers = Physics.DefaultRaycastLayers;
    [SerializeField, Min(0f)] private float _surfaceMargin = 0.02f;
    [SerializeField] private bool _hintEnabled = true; // activa o desactiva el destello (ese feeback visual)

    private ParticleSystem _particles;
    private bool _isVisible;    // indica si el destello se está reproduciendo
    #endregion

    #region Unity Methods
    private void Awake()
    {
        _particles = GetComponent<ParticleSystem>();

        ParticleSystem.MainModule main = _particles.main;
        main.playOnAwake = false;

        // se detiene la reproducción de partículas y se limpian/eliminan
        _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void LateUpdate()
    {
        SetVisible(CanShowGlint());
    }

    private void OnDisable()
    {
        _isVisible = false;
        if (_particles != null)
        {
            _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
    #endregion

    #region Visibility Methods
    // Comprueba si se puede reproducir el destello
    private bool CanShowGlint()
    {
        // comprobaciones
        if (!_hintEnabled || Time.timeScale <= 0f || _playerCamera == null
                || !_playerCamera.isActiveAndEnabled)
        {
            return false;
        }

        // se calcula la distancia entre la cámara del jugador y el objeto con el destello
        Vector3 origin = _playerCamera.transform.position;
        // vector que va desde la cámara al objeto
        Vector3 direction = transform.position - origin;
        float distance = direction.magnitude;

        // si el jugador está muy lejos, no se ve
        if (distance > _maxDistance)
        {
            return false;
        }

        // se comprueba si el objeto está dentro de la vista de la cámara (el recuadro de lo
        // que ve el jugador)
        Vector3 viewportPoint = _playerCamera.WorldToViewportPoint(transform.position);
        if (viewportPoint.z <= 0f || viewportPoint.x < 0f || viewportPoint.x > 1f
                || viewportPoint.y < 0f || viewportPoint.y > 1f)
        {
            return false;
        }

        // se comprueban los obstáculos
        float rayDistance = Mathf.Max(0f, distance - _surfaceMargin);
        // en caso de que el jugador esté muy cerca, no hace falta comprobarlos
        if (rayDistance <= 0f)
        {
            return true;
        }

        // se lanza el rayo para detectar posibles obstáculos
        bool blocked = Physics.Raycast( 
            origin,                 // sale desde la cámara
            direction.normalized,   // apunta al destello
            rayDistance,            // hasta qué distance se comprueba
            _osbtacleLayers,
            QueryTriggerInteraction.Ignore
        );

        return !blocked;        
    }

    // Inicia o detiene la reproducción del destello
    private void SetVisible(bool visible)
    {
        // si estado no ha cambiado, sale
        if (_isVisible == visible) return;

        _isVisible = visible;
        // si se muestra, se inicia su reproducción
        if (visible)
        {
            _particles.Play(true);
        }
        else
        {
            _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    // Permite activar o desactivar el destello
    public void SetHintEnabled(bool enabled)
    {
        _hintEnabled = enabled;
        if (!enabled)
        {
            SetVisible(false);
        }
    }
    #endregion
}
