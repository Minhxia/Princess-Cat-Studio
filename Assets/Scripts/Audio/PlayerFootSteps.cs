using UnityEngine;

[DisallowMultipleComponent]

// Clase que reproduce sonidos de pisadas teniendo en cuenta el desplazamiento 
// horizontal del jugador
public class PlayerFootSteps : MonoBehaviour
{
    #region Variables
    [Header("Sound Clips")]
    [SerializeField] private AudioClip[] walkClips;
    [SerializeField] private AudioClip[] runClips;

    [Header("Rhythm")]
    [SerializeField, Min(0.01f)] private float _walkInterval = 0.5f;
    [SerializeField, Min(0.01f)] private float _runInterval = 0.3f;
    [SerializeField, Min(0.1f)] private float _runSpeed = 4f;

    [Header("Detection")]
    [SerializeField, Min(0f)] private float _walkRadius = 5f;
    [SerializeField, Min(0f)] private float _runRadius = 12f;

    // si el jugador está agachado, sus pasos hacen menos ruido y es más difícil detectarle
    [Header("Crouching")]
    [SerializeField, Range(0f, 1f)] private float _crouchVolumeMultiplier = 0.35f;
    [SerializeField, Min(0f)] private float _crouchRadius = 1.5f;

    private CharacterController _characterController;
    private CharacterMovement3D _characterMovement;
    private AudioSource _audioSource;
    private float _stepTimer;               // contador de pasos
    private float _originalFootstepVolume;  // volumen original de los pasos (ya que cambia al agaccharse)
    #endregion

    #region Unity Methos
    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _characterMovement = GetComponent<CharacterMovement3D>();
        _audioSource = GetComponent<AudioSource>();

        if (_characterController == null || _characterMovement == null || _audioSource == null)
        {
            Debug.LogWarning("[PlayerFootSteps] Faltan CharacterController, CharacterMovemente3D o AudioSource.", this);
            enabled = false;
            return;
        }

        _originalFootstepVolume = _audioSource.volume;
        
        // se configura el audio
        _audioSource.playOnAwake = false;
        _audioSource.loop = false;
        _audioSource.Stop();
    }

    // es importante usar LateUpdate en lugar de Update, para dar tiempo a que
    // PlayerController realice su Update y así se lea el resultado del movimiento
    // durante el fotograma
    private void LateUpdate()
    {
        if (_characterController == null || !_characterController.enabled)
        {
            _stepTimer = 0f;
            return;
        }

        // se comprueba si el juego está pausado (menú de pausa)
        if (Time.timeScale <= 0f)
        {
            _stepTimer = 0f;
            if (_audioSource != null && _audioSource.isPlaying)
            {
                _audioSource.Stop();
            }
            return;
        }

        // se calcula la velocidad horizontal del jugador
        Vector3 horizontalVelocity = _characterController.velocity;
        horizontalVelocity.y = 0f;  // no interesa la componente vertical
        float horizontalSpeed = horizontalVelocity.magnitude;

        // se comprueba si el jugador está en el aire o prácticamente quieto
        if (!_characterController.isGrounded || horizontalSpeed <= 0.1f)
        {
            _stepTimer = 0f;
            return;
        }

        // finalmente, se comprueba si el jugador está caminando o corriendo
        bool running = horizontalSpeed >= _runSpeed;
        // se elige el tiempo que pasa entre pisadas
        float interval = Mathf.Max(0.01f, running ? _runInterval : _walkInterval);

        _stepTimer += Time.deltaTime;
        if(_stepTimer >= interval)
        {
            // un solo paso por frame, no se acumulan las pisadas pendientes!
            _stepTimer = 0f;
            MakeFootStep(running);
        }
    }

    private void OnDisable()
    {
        _stepTimer = 0f;
        if (_audioSource != null)
        {
            _audioSource.Stop();
        }
        
    }
    #endregion

    #region Audio Methods
    // Da un paso
    private void MakeFootStep(bool running)
    {
        bool crounching = _characterMovement.IsCrouching;

        running = running && !crounching;   // agachado se usan los mismo clips que al andar
        AudioClip clip = PickClip(running ? runClips : walkClips);
        // en caso de que esté corriendo y no haya audios disponibles, se usan
        // los de andar
        if(clip == null && running)
        {
            clip = PickClip(walkClips);
        }

        if (_audioSource != null && clip != null)
        {
            _audioSource.Stop();

            // se ajusta el volumen en caso de estar agachado
            float volumeMultiplier = crounching ? _crouchVolumeMultiplier : 1f;
            _audioSource.volume = _originalFootstepVolume * volumeMultiplier;

            _audioSource.clip = clip;
            _audioSource.Play();
        }

        // aunque no haya un clip de audio asignado, se sigue avisando a los enemigos
        // de la pisada (ruido) del jugador
        // (primero se comprueba si está agachado, si es así, se usa el radio/intensidad 
        // correspondientes. Si no, se mantiene la comparación inicial: está corriendo o andando?)
        float radius = crounching ? _crouchRadius : (running ? _runRadius : _walkRadius);
        float loudness = crounching ? 0.15f : (running ? 1f : 0.4f);
        NoiseEvent.NoiseSystem.EmitNoise(transform.position, radius, loudness, gameObject);
    }

    // Elige aleatoriamente un clip de sonido (andar, correr) entre varios, para dar
    // más realismo
    private AudioClip PickClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0)
        {
            Debug.Log("[PlayerFootSteps] No hay audios entre los que elegir.");
            return null;
        }

        // se elige un clip de sonido aleatoriamente
        return clips[Random.Range(0, clips.Length)];
    }
    #endregion
}
