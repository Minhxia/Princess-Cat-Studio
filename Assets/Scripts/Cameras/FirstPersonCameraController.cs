using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
// Orden de ejecución del script:
//[DefaultExecutionOrder(-1)] // para evitar que el script de las manos lea el fotograma anterior

// Clase que controla la cámara en primera persona
public class FirstPersonCameraController : MonoBehaviour
{
    #region Variables
    [Header("Camera")]
    [SerializeField] private Transform _firstPersonCamera;
    [SerializeField, Range(0.5f, 0.9f)] private float _eyeHeightRatio = 0.8f;

    [Header("Look")]
    [SerializeField] private float _mouseSensitivity = 0.1f;
    [SerializeField] private float _maxVerticalAngle = 80f;

    [Header("Head Bob")]
    [SerializeField] private float _walkBobFrequency = 1.6f;
    [SerializeField] private float _walkBobAmplitude = 0.02f;
    [SerializeField] private float _runBobFrequency = 2.6f;
    [SerializeField] private float _runBobAmplitude = 0.035f;
    [SerializeField] private float _runSpeedThreshold = 4.5f;
    [SerializeField] private float _bobReturnSpeed = 0.15f;

    private float _bobPhase;
    private float _bobOffset;

    private CharacterController _characterController;
    private float _verticalAngle;
    private bool _waitingForCaptureClickRelease = true;

    public bool CanUseHandObjects { get; private set; } 
    #endregion

    #region Unity Methods
    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();

        #if !UNITY_EDITOR && UNITY_WEBGL
            // se sincroniza el estado del cursor en Unity con el del navegador, es decir,
            // si el cursor se desbloquea en el navegador al pulsar ESC, se desbloquea también en Unity
            WebGLInput.stickyCursorLock = false;
        #endif

        UnlockCursor();   
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        // se desbloquea el cursor al pulsar ESC
        if (keyboard !=null && keyboard.escapeKey.wasPressedThisFrame)
        {
            UnlockCursor();
            return;
        }

        // en caso de que no haya un ratón conectado, se desactiva usar objetos con las manos
        if (mouse == null)
        {
            CanUseHandObjects = false;
            return;
        }

        // si el cursor está desbloqueado, se espera a que se haga click izquierdo
        // antes de capturarlo / bloquearlo
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            CanUseHandObjects = false;
            if (mouse.leftButton.wasPressedThisFrame)
            {
                LockCursor();
            }
            return;
        }

        // se espera a que se suelte el click izquierdo antes de permitir usar 
        // objetos con las manos, así se evita que se active el objeto sin querer
        if (_waitingForCaptureClickRelease)
        {
            CanUseHandObjects = false;

            if (!mouse.leftButton.isPressed)
            {
                _waitingForCaptureClickRelease = false;
            }
        }
        else
        {
            CanUseHandObjects = true;
        }

        // se lee cuánto se ha movido el ratón desde el último fotograma (acumulado)
        Vector2 mouseMovement = mouse.delta.ReadValue();
        // se rota la cámara mediante el transform del padre (personaje)
        // en el eje Y (izda/dcha)
        transform.Rotate(0f, mouseMovement.x * _mouseSensitivity, 0f);
         // se rota la propia cámara en el eje X (arriba/abajo)
        _verticalAngle -= mouseMovement.y * _mouseSensitivity;
        _verticalAngle = Mathf.Clamp(
            _verticalAngle,
            -_maxVerticalAngle,
            _maxVerticalAngle
        );
        _firstPersonCamera.localRotation = Quaternion.Euler(_verticalAngle, 0f, 0f);  
    }

    private void LateUpdate()
    {
        // se ajusta la cámara a la altura de los ojos del personaje, que cambia al agacharse
        float feetHeight = _characterController.center.y - _characterController.height / 2f;
        float eyeHeight = feetHeight + _characterController.height * _eyeHeightRatio;

        UpdateHeadBob();
        
        // se conserva la posición local de la cámara en X y Z, y se ajusta solo la Y
        Vector3 cameraPosition = _firstPersonCamera.localPosition;
        cameraPosition.y = eyeHeight + _bobOffset;
        _firstPersonCamera.localPosition = cameraPosition;
    }

    private void OnDisable()
    {
        UnlockCursor();
    }
    #endregion

    #region Other Methods
    // Cursor bloqueado: el cursor se oculta y al mover el ratón se gira la cámara.
    // Cursor desbloqueado/liberado: el cursor se muestra y se puede hacer click en los menús.

    // Bloquea el cursor y lo hace invisible
    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        _waitingForCaptureClickRelease = false;
        CanUseHandObjects = true;

        Debug.Log("[FirstPersonCameraController] Cursor bloqueado y oculto. Se puede usar objetos con las manos? " + CanUseHandObjects);
    }

    // Desbloquea el cursor y lo hace visible
    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        _waitingForCaptureClickRelease = true;
        CanUseHandObjects = false;

        Debug.Log("[FirstPersonCameraController] Cursor desbloqueado y visible. Se puede usar objetos con las manos? " + CanUseHandObjects);
    }

    // Actualiza el efecto "head bob" (movimiento de la cabeza al caminar/correr)
    private void UpdateHeadBob()
    {
        Vector3 horizontalVelocity = _characterController.velocity;
        horizontalVelocity.y = 0f;

        float horizontalSpeed = horizontalVelocity.magnitude;

        // se aplica el efecto solo si el personaje está en el suelo y se mueve
        if (_characterController.isGrounded && horizontalSpeed > 0.1f)
        {
            bool isRunning = horizontalSpeed > _runSpeedThreshold;
            float frequency = isRunning ? _runBobFrequency : _walkBobFrequency;
            float amplitude = isRunning ? _runBobAmplitude : _walkBobAmplitude;

            // el seno produce una pequeña oscilación de la cámara
            _bobPhase += Time.deltaTime * frequency * 2f * Mathf.PI;
            _bobOffset = Mathf.Sin(_bobPhase) * amplitude;
        }
        // si el personaje se para o salta, la oscilación vuelve a 0 de manera gradual
        else
        {
            _bobPhase = 0f;
            _bobOffset = Mathf.MoveTowards(
                _bobOffset, 
                0f, 
                _bobReturnSpeed * Time.deltaTime
            );
        }
    }
    #endregion
}
