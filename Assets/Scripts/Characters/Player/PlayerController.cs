using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterMovement3D), typeof(PlayerStamina))]

// Clase que gestiona el control del jugador (lectura de entradas de teclado)
public class PlayerController : MonoBehaviour
{
    #region Variables
    [Header("Movement keys")]
    [SerializeField] private Key _forwardKey = Key.W;
    [SerializeField] private Key _backwardKey = Key.S;
    [SerializeField] private Key _leftKey = Key.A;
    [SerializeField] private Key _rightKey = Key.D;

    [Header("Action keys")]
    [SerializeField] private Key _jumpKey = Key.Space;
    [SerializeField] private Key _runKey = Key.LeftShift;
    [SerializeField] private Key _crouchKey = Key.LeftCtrl;

    private bool _crouchToggled;

    private CharacterMovement3D _characterMovement;
    private PlayerStamina _stamina;
    #endregion

    #region Unity Methods
    private void Awake()
    {
        _characterMovement = GetComponent<CharacterMovement3D>();
        _stamina = GetComponent<PlayerStamina>();
    }

    private void Update()
    {
        // se obtiene el teclado actual
        Keyboard keyboard = Keyboard.current;

        // TODO: revisar más adelante si se quiere permitir el uso de gamepads
        // si no hay un teclado, se detiene el movimiento del jugador
        if (keyboard == null)
        {
            _characterMovement.SetMoveDirection(Vector3.zero);
            _characterMovement.SetRunning(false);
            _characterMovement.SetCrouching(false);
            return;
        }

        // se obtiene la dirección de movimiento en función de las teclas presionadas
        float horizontal =
            (IsPressed(keyboard, _rightKey) ? 1f : 0f) -
            (IsPressed(keyboard, _leftKey) ? 1f : 0f);
        float vertical =
            (IsPressed(keyboard, _forwardKey) ? 1f : 0f) -
            (IsPressed(keyboard, _backwardKey) ? 1f : 0f);
        // el giro de la cámara también afecta a la dirección de movimiento!
        Vector3 moveDirection = transform.right * horizontal + transform.forward * vertical;
        _characterMovement.SetMoveDirection(moveDirection);

        _characterMovement.SetRunning(IsPressed(keyboard, _runKey));

        if (_crouchKey != Key.None && keyboard[_crouchKey].wasPressedThisFrame)
        {
            // si el jugador no está agachado, se agacha
            if (!_crouchToggled)
            {
                _crouchToggled = true;
            }
            // si el jugador está agachado y ya puede levantarse, se mantiene agachado
            // (así se evita que el jugador se levante automáticamente al salir de debajo del obstáculo,
            // y se mantiene agachado hasta que el jugador decida pulsar la tecla de agacharse otra vez)
            // es decir, como estaba antes planteado, si se pulsaba Crtl estando agachado debajo
            // del techo, al salir de debajo del mismo, se levantaba solo, tenía memoria, no queremos eso
            else if (_characterMovement.CanStandUp())
            {
                _crouchToggled = false;
            }

            Debug.Log("[PlayerController] Está agachado: " + _crouchToggled + ", puede levantarse? " + _characterMovement.CanStandUp());
        }
        _characterMovement.SetCrouching(_crouchToggled);

        // se actualiza la estamina del jugador y se indica si está corriendo
        bool canRun = moveDirection.sqrMagnitude > 0f && !_crouchToggled;
        bool isRunning = _stamina.UpdateStamina(IsPressed(keyboard, _runKey), canRun);
        _characterMovement.SetRunning(isRunning);
        //Debug.Log("[PlayerController] Está corriendo: " + isRunning + ", puede correr? " + canRun + ", estamina fill: " + _stamina.FillAmount.ToString("F2"));

        if (_jumpKey != Key.None && keyboard[_jumpKey].wasPressedThisFrame)
        {
            _characterMovement.RequestJump();
        }
    }

    private void OnDisable()
    {
        if (_characterMovement == null) return;

        // se detiene el movimiento del jugador al desactivar el script
        _characterMovement.SetMoveDirection(Vector3.zero);
        _characterMovement.SetRunning(false);
        _characterMovement.SetCrouching(false);

        _crouchToggled = false;
    }

    #endregion

    #region Input Methods
    // Método auxiliar para comprobar si una tecla está presionada
    // (no se comprueba nada si no se ha pulsado nada)
    private bool IsPressed(Keyboard keyboard, Key key)
    {
        return key != Key.None && keyboard[key].isPressed;
    }

    #endregion

}
