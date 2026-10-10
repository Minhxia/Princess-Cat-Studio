using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]

[RequireComponent(typeof(CharacterMovement3D), typeof(PlayerStamina), typeof(PlayerItemInteraction))]
[RequireComponent(typeof(PlayerHands), typeof(FirstPersonCameraController))]

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
    [SerializeField] private Key _pickupKey = Key.F;
    [SerializeField] private Key _dropLeftKey = Key.Q;
    [SerializeField] private Key _dropRightKey = Key.E;

    private bool _crouchToggled;
    private CharacterMovement3D _characterMovement;
    private PlayerStamina _stamina;

    private PlayerItemInteraction _itemInteraction;
    private PlayerHands _hands;
    private FirstPersonCameraController _firstPersonCameraController;
    private bool _readyForHandInput;
    #endregion

    #region Unity Methods
    private void Awake()
    {
        _characterMovement = GetComponent<CharacterMovement3D>();
        _stamina = GetComponent<PlayerStamina>();
        _itemInteraction = GetComponent<PlayerItemInteraction>();
        _hands = GetComponent<PlayerHands>();
        _firstPersonCameraController = GetComponent<FirstPersonCameraController>();
    }

    private void Update()
    {
        UpdateHandUse();

        // se obtiene el teclado actual
        Keyboard keyboard = Keyboard.current;

        // TODO: revisar más adelante si se quiere permitir el uso de gamepads o cambiar los controles
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

        // agacharse
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

        // correr
        // se actualiza la estamina del jugador y se indica si está corriendo
        bool canRun = moveDirection.sqrMagnitude > 0f && !_crouchToggled;
        bool isRunning = _stamina.UpdateStamina(IsPressed(keyboard, _runKey), canRun);
        _characterMovement.SetRunning(isRunning);
        //Debug.Log("[PlayerController] Está corriendo: " + isRunning + ", puede correr? " + canRun + ", estamina fill: " + _stamina.FillAmount.ToString("F2"));

        // salto
        if (_jumpKey != Key.None && keyboard[_jumpKey].wasPressedThisFrame)
        {
            _characterMovement.RequestJump();
        }

        // coger objetos
        if (_pickupKey != Key.None && keyboard[_pickupKey].wasPressedThisFrame)
        {
            _itemInteraction.TryPickup();
        }
        // soltar objetos
        if (_dropLeftKey != Key.None && keyboard[_dropLeftKey].wasPressedThisFrame)
        {
            _itemInteraction.TryDropLeft();
        }
        if (_dropRightKey != Key.None && keyboard[_dropRightKey].wasPressedThisFrame)
        {
            _itemInteraction.TryDropRight();
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

    // Lee los clicks y los "envía" a la mano adecuada
    private void UpdateHandUse()
    {
        Mouse mouse = Mouse.current;
        // se comprueba si se pueden usar objetos
        bool canUse = mouse != null && _hands != null 
            && _firstPersonCameraController != null
            && _firstPersonCameraController.isActiveAndEnabled
            && _firstPersonCameraController.CanUseHandObjects
            && Cursor.lockState == CursorLockMode.Locked;

        // en caso de que no se pueda, termina su uso
        if (!canUse)
        {
            StopUsingHands();
            return;
        }

        // se controla que al capturar al cursor no se active un objeto de una mano
        if (!_readyForHandInput)
        {
            if (!mouse.leftButton.isPressed && !mouse.rightButton.isPressed)
            {
                _readyForHandInput = true;
            }
            return;
        }

        // uso del objeto con los clicks (se pueden usar los dos objetos a la vez)
        if (mouse.leftButton.wasPressedThisFrame) {
            Debug.Log("[PlayerController] Pulsación del click izquierdo.");
            _hands.BeginUseLeft();
        }
        if (mouse.leftButton.wasReleasedThisFrame)  _hands.EndUseLeft();

        if (mouse.rightButton.wasPressedThisFrame)  _hands.BeginUseRight();
        if (mouse.rightButton.wasReleasedThisFrame) _hands.EndUseRight();
    }

    // Termina el uso de un objeto cuando ya no se pueden usar más (por ejemplo, en los menús)
    private void StopUsingHands()
    {
        _readyForHandInput = false;

        if (_hands == null) return;

        _hands.EndUseLeft();
        _hands.EndUseRight();
    }
    #endregion

}
