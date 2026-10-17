using UnityEngine;
[DisallowMultipleComponent]

// Clase que reproduce pisadas a partir del desplazamiento horizontal del jugador
public class PlayerFootSteps : MonoBehaviour 
{
    #region Variables
    [Header("Sound Clips")]
    [SerializeField] private AudioClip[] walkClips;
    [SerializeField] private AudioClip[] runClips;

    [Header("Rhythm")]
    [SerializeField, Min(0.01f)] float walkInterval = 0.5f;
    [SerializeField, Min(0.01f)] float runInterval = 0.3f;
    [SerializeField, Min(0.1f)] float runSpeed = 4f;

    [Header("Detection")]
    [SerializeField, Min(0f)] private float walkRadius = 5f;
    [SerializeField, Min(0f)] private float runRadius = 12f;

    private CharacterController _characterController;
    private AudioSource _audioSource;
    private float _stepTimer;
    #endregion

    #region Unity Methos
    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _audioSource = GetComponent<AudioSource>();

        if (_characterController == null || _audioSource == null)
        {
            Debug.LogWarning("[PlayerFootSteps] Faltan CharacterController o AudioSource.", this);
            enabled = false;
            return;
        }
        
        // se configura el audio
        _audioSource.playOnAwake = false;
        _audioSource.loop = false;
        _audioSource.Stop();
    }

    // es importante usar LateUpdate en lugar de Update, para dar tiempo a que
    // PlayerController realice su Update y así se lea el resultado del movimiento
    // actualizado
    private void LateUpdate()
    {
        if (_characterController == null || _audioSource == null) return;

        bool moving = _characterController.isGrounded && _characterController.velocity.magnitude > 0.1f;

        if (!moving )
        {
            _stepTimer = 0f;
            return;
        }

        bool running = _characterController.velocity.magnitude >= runSpeed;
        float interval = running ? runInterval : walkInterval;
        _stepTimer += Time.deltaTime;

        if(_stepTimer >= interval)
        {
            _stepTimer = 0f;
            MakeFootStep(running);
        }
    }
    #endregion

    private void MakeFootStep(bool running)
    {
        AudioClip[] clips = running ? runClips : walkClips;

        if(clips != null && clips.Length > 0  )
        {
            AudioClip clip = clips[Random.Range(0, clips.Length)];
            _audioSource.PlayOneShot(clip);
        }

        float radius = running ? runRadius : walkRadius;
        float loudness = running ? 1f : 0.4f;
        NoiseEvent.NoiseSystem.EmitNoise(transform.position, radius, loudness, gameObject);
    }
}
