using UnityEngine;

[DisallowMultipleComponent]

// AudioLowPassFilter permite atenuar las frecuencias agudas de los sonidos
[RequireComponent(typeof(AudioSource), typeof(AudioLowPassFilter))]

// Clase que reduce el volumen y atenúa los sonidos para se que escuchen menos.
// Es decir, simula cómo cambia un sonido cuando hay un obstáculo entre su origen
// y quien lo escucha. Así se consiguen sonidos "amortiguados".
// Esta clase debe llevarla equipada el enemigo!
public class SoundOclusion : MonoBehaviour
{
    #region Variables
    [Header("Listener")]
    [SerializeField] private Transform _listener; // posición de quien lo escucha

    [Header("Obstacles")]
    [SerializeField] private LayerMask _occlusionLayer;

    // valores del sonido sin y con obstáculos
    [Header("Sound without obstacles")]
    [SerializeField, Range(0f, 1f)] private float _clearVolumen = 1f;
    [SerializeField, Range(500f, 22000f)] private float _clearCutoff = 22000f;

    [Header("Sound with obstacles")]
    [SerializeField, Range(0f, 1f)] private float _blockedVolumen = 0.3f;
    [SerializeField, Range(500f, 10000)] private float _blockedCutoff = 1200f;

    // velocidad de transición (rapidez con la que cambian el volumen y el filtro)
    [Header("Transition")]
    [SerializeField, Min(0.1f)] private float _smoothingSpeed = 5f;

    // a partir de esta distancia, el volumen se reduce hasta 0
    [Header("Limit distance")]
    [SerializeField] private float _maxDistanceVolume = 20f;

    private AudioSource _audioSource;
    private AudioLowPassFilter _lowPassFilter;
    private float _originalVolume;
    private float _targetVolume;    // multiplicado del volumen
    private float _targetCutoff;    // frecuencia de corte
    #endregion

    #region Unity Methods
    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _lowPassFilter = GetComponent<AudioLowPassFilter>();

        _originalVolume = _audioSource.volume;
        _targetVolume = _clearVolumen;
        _targetCutoff = _clearCutoff;

        _lowPassFilter.cutoffFrequency = _clearCutoff;

    }
    private void Start()
    {
        if (_listener == null && Camera.main != null)
        {
            _listener = Camera.main.transform;  // le he dado la etiqueta a FirstPersonCamera
        }

        if (_listener == null)
        {
            Debug.LogWarning("[SoundOcclusion] Falta asignar la posición del AudioListener activo.", this);
        }
    }

    // Se encarga de la modificación gradual de los sonidos
    private void Update()
    {
        if (_listener == null) return;

        UpdateOclusion();

        // se calcula el factor de transición del fotograma
        // (se usa Clamp01 porque AudioSource.volume se limita al rango 0-1)
        float blend = Mathf.Clamp01(Time.deltaTime * _smoothingSpeed);

        // 
        _audioSource.volume = Mathf.Lerp(
            _audioSource.volume,                            // valor actual del volumen
            Mathf.Clamp01(_originalVolume * _targetVolume), // valor objetivo
            blend
            );

        _lowPassFilter.cutoffFrequency = Mathf.Lerp(
            _lowPassFilter.cutoffFrequency,
            _targetCutoff,
            Time.deltaTime * _smoothingSpeed
            );
    }

    // Reseteo
    private void OnDisable()
    {
        if (_audioSource != null)
        {
            _audioSource.volume = _originalVolume;
        }

        if (_lowPassFilter != null)
        {
            _lowPassFilter.cutoffFrequency = _clearCutoff;
        }
    }
    #endregion

    // Calcula los valores del sonido según la distancia y los obstáculos
    private void UpdateOclusion()
    {
        Vector3 origin = transform.position;        // emisor
        Vector3 destination = _listener.position;   // receptor

        float distance = Vector3.Distance(origin, destination);
        // en caso de que el emisor y el receptro estén demasiado lejos, sale
        if (distance > _maxDistanceVolume)
        {
            _targetVolume = 0f;
            _targetCutoff = _blockedCutoff;
            return;
        }

        // se comprueban los obstáculos (colliders entre el origen y el oyente)
        bool blocked = Physics.Linecast(
            origin,
            destination,
            _occlusionLayer,
            QueryTriggerInteraction.Ignore);

        _targetVolume = blocked ? _blockedVolumen : _clearVolumen;
        _targetCutoff = blocked ? _blockedCutoff : _clearCutoff;
    }
}
