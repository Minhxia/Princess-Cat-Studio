using UnityEngine;

[RequireComponent(typeof(CharacterController))]
// Clase que gestiona el movimiento de un personaje en 3D,
// incluyendo caminar, correr, agacharse y saltar
public class CharacterMovement3D : MonoBehaviour
{
    #region Variables
    [Header("Speeds")]
    [SerializeField] private float _walkSpeed = 3f;
    [SerializeField] private float _runSpeed = 6f;
    [SerializeField] private float _crouchSpeed = 1.5f;

    [Header("Jump")]
    [SerializeField] private float _jumpHeight = 1.5f;
    [SerializeField] private float _gravity = -9.81f;

    [Header("Crouch")]
    [SerializeField] private float _crouchHeight = 1f;

    private CharacterController _characterController;
    private Vector3 _moveDirection;
    private Vector3 _standingCenter;

    private float _standingHeight;
    private float _verticalVelocity;

    private bool _wantsToRun;
    private bool _wantsToCrouch;
    private bool _jumpRequested;
    private bool _isCrouching;
    #endregion

    #region Unity Methods
    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();

        _standingHeight = _characterController.height;
        _standingCenter = _characterController.center;

        // se controla el valor mínimo de altura al agacharse 
        // en este caso, la cápsula no puede ser más baja que su diámetro (formado 
        // por los dos radios de las semiesferas de arriba y de abajo) para
        // evitar que la altura acabe siendo demasiado baja o negativa,
        // y/o de problemas el collider
        // TODO: revisar cuando se incluyan los sprites de los personajes: player y enemies, ya que está pensado para una cápsula
        _crouchHeight = Mathf.Min(
            _standingHeight,
            Mathf.Max(_crouchHeight, _characterController.radius * 2f)
        );
    }

    private void Update()
    {
        // se actualiza el estado de agachado del personaje
        UpdateCrouch();

        // se actualiza la velocidad vertical del personaje (para el salto)
        UpdateVerticalVelocity();

        // se determina la velocidad de movimiento del personaje
        float speed = _isCrouching ? _crouchSpeed // si está agachado, se usa la velocidad de agachado
                        : (_wantsToRun ? _runSpeed : _walkSpeed); // si no está agachado, se usa la velocidad de correr o caminar según corresponda
        // se calcula la velocidad final, que combina la velocidad de movimiento 
        // horizontal con la vertical
        Vector3 velocity = _moveDirection * speed + Vector3.up * _verticalVelocity;

        // finalmente, se mueve el personaje según la velocidad final calculada
        _characterController.Move(velocity * Time.deltaTime);
    }
    #endregion

    #region Crouch Methods
    // Actualiza el estado de agachado del personaje    
    private void UpdateCrouch()
    {
        // si el personaje quiere agacharse y no está ya agachado
        if (_wantsToCrouch && !_isCrouching)
        {
            SetCharacterColliderHeight(_crouchHeight);
            _isCrouching = true;
        }
        // si el personaje quiere levantarse, está agachado y puede levantarse
        else if (!_wantsToCrouch && _isCrouching && CanStandUp())
        {
            SetCharacterColliderHeight(_standingHeight);
            _isCrouching = false;
        }
    }

    // Ajusta la altura del collider del CharacterController y su centro 
    // al agacharse o levantarse
    private void SetCharacterColliderHeight(float height)
    {
        // se actualiza la altura
        _characterController.height = height;
        // se actualiza el centro con la nueva altura, manteniendo la posición en X y Z
        _characterController.center = new Vector3(
            _standingCenter.x,
            height / 2f,
            _standingCenter.z
        );
    }

    // TODO: comprobar si con los sprites de los personajes sigue funcionado bien
    // Comprueba si el personaje puede levantarse
    public bool CanStandUp()
    {
        // se genera una cápsula imaginaria que representa el espacio que ocuparía 
        // el personaje si se levantara (es ligeramente más pequeña, de ahí el 0.95)
        // TODO: revisar si interesa multiplicar por 0.95, ya que la cápsula es un poco más pequeña que la real. Puede dar problemas con obstáculos muy ajustados
        float radius = _characterController.radius * 0.95f;

        // se calcula la posición del centro del personaje en el mundo,
        // y la posición de los pies
        Vector3 standingCenterWorld = transform.TransformPoint(_standingCenter);
        // Y de los pies = Y del centro − media altura
        Vector3 feetWorld = standingCenterWorld - Vector3.up * (_standingHeight / 2f);

        // se empieza a comprobar desde la parte alta del personaje agachado, para evitar
        // que el collider del personaje detecte una colisión con el suelo
        float lowerHeight = Mathf.Min(
            _crouchHeight, 
            _standingHeight - _characterController.radius // altura del centro del extremo redondeado superior de la cápsula
        );
        // son los centros de los extremos redondeados de la cápsula, no el borde inferior
        // y superior de la misma
        Vector3 lowerPoint = feetWorld + Vector3.up * lowerHeight;
        Vector3 upperPoint = feetWorld + Vector3.up * (_standingHeight - _characterController.radius);
       
        // TODO: revisar si al poner el sprite de los personajes, el collider deja de ser una cápsula
        Collider[] hits = Physics.OverlapCapsule(
            lowerPoint, upperPoint, radius,
            Physics.AllLayers,
            QueryTriggerInteraction.Ignore
        );
        foreach (Collider hit in hits)
        {
            // si el collider que se ha detectado no es un hijo del personaje, 
            // significa que hay un obstáculo que impide levantarse
            if (!hit.transform.IsChildOf(transform))
            {
                Debug.Log("[CharacterMovement3D] No se puede levantar, hay un obstáculo: " + hit.name);
                return false;
            }
        }
        Debug.Log("[CharacterMovement3D] Se puede levantar, no hay obstáculos");
        return true;
    }
    #endregion

    #region Jump Methods
    // Actualiza la velocidad vertical del personaje (para el salto),
    // no lo mueve como tal
    private void UpdateVerticalVelocity()
    {
        // se compruea si el personaje ha tocado el suelo en su último movimiento
        bool isGrounded = _characterController.isGrounded;

        // si el personaje está en el suelo y la velocidad es negativa (hacia abajo)
        if(isGrounded && _verticalVelocity < 0f)
        {
            // el personaje estaba cayendo y acaba de tocar el suelo, por lo que se sustituye
            // la velocidad vertical (que puede ser grande, -12) por un valor negativo 
            // pequeño para que el personaje se mantenga en el suelo (no lo atraviese o rebote)
            _verticalVelocity = -2f;
        }

        // si el personaje ha solicitado un salto, está en el suelo y no está agachado
        if(_jumpRequested && isGrounded && !_isCrouching)
        {
            // se calcula la velocidad vertical necesaria para alcanzar la altura de salto
            _verticalVelocity = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
        }

        // TODO: cambiar esto en caso de querer permitir doble salto, triple salto, etc.
        _jumpRequested = false; // no hay doble salto

        // se aplica la gravedad a la velocidad vertical
        _verticalVelocity += _gravity * Time.deltaTime;
    }
    #endregion

    #region Other Public Methods
    // Establece la dirección de movimiento del personaje
    public void SetMoveDirection(Vector3 direction)
    {
        direction.y = 0f; // no interesa la velocidad vertical, porque se calcula aparte
        _moveDirection = Vector3.ClampMagnitude(direction, 1f); // se normaliza
    }

    // Establece si el personaje quiere correr
    public void SetRunning(bool running)
    {
        _wantsToRun = running;
    }

    // Establece si el personaje quiere agacharse
    public void SetCrouching(bool crouching)
    {
        _wantsToCrouch = crouching;
    }

    // Indica si el personaje quiere saltar
    public void RequestJump()
    {
        _jumpRequested = true;
    }
    #endregion
}