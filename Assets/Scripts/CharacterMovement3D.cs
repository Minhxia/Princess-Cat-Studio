using UnityEngine;

[RequireComponent(typeof(CharacterController))]
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
    private bool CanStandUp()
    {
        // se comprueba el volumen que ocuparía el personaje de pie
        // (se supone que la escala es 1,1,1)
        // TODO: revisar si intersa multiplicar por 0.95, ya que la cápsula es un poco más pequeña que la real. Puede dar problemas con obstáculos muy ajustados
        float radius = _characterController.radius * 0.95f;

        // se calcula la distancia desde el centro de la cápsula hasta el centro
        // de cada extremo redondo (semiesferas)
        float halfSegment = _standingHeight / 2f - _characterController.radius;

        Vector3 center = transform.TransformPoint(_standingCenter); // posición local a posición del mundo
        Vector3 bottom = center - Vector3.up * halfSegment; // bottom -> Y = 0,5
        Vector3 top = center + Vector3.up * halfSegment;    // top    -> Y = 1,5
        // son los centros de los extremos redondos (semiesferas), no los puntos más
        // bajo y más alto de la cápsula

        // se comprueba si hay colisiones con otros colliders en el volumen que 
        // ocuparía el personaje de pie (en este caso, la cápsula)
        // TODO: revisar si al poner el sprite de los personajes, el collider deja de ser una cápsula
        Collider[] hits = Physics.OverlapCapsule(
            bottom, top, radius,
            Physics.AllLayers,
            QueryTriggerInteraction.Ignore
        );
        foreach (Collider hit in hits)
        {
            // si el collider que se ha detectado no es un hijo del personaje, 
            // significa que hay un obstáculo que impide levantarse
            if (!hit.transform.IsChildOf(transform))
            {
                return false;
            }
        }
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

    #region Public Methods
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