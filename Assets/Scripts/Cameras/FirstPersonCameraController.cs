using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
// Orden de ejecución del script:
//[DefaultExecutionOrder(-1)] // para evitar que el script de las manos lea el fotograma anterior

public class FirstPersonCameraController : MonoBehaviour
{
    #region Variables
    [Header("Camera")]
    [SerializeField] private Transform _firstPersonCamera;
    [SerializeField, Range(0.5f, 0.9f)] private float _eyeHeightRatio = 0.8f;

    [Header("Look")]
    [SerializeField] private float _mouseSensitivity = 0.1f;
    [SerializeField] private float _maxVerticalAngle = 80f;

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
        {
            CanUseHandObjects = false;
            return;
        }
        
    }

    #endregion

    #region Other Methods
    // Desbloquea el cursor y lo hace visible
    // Cursor bloqueado: el cursor se oculta y mover el ratón gira la cámara.
    // Cursor desbloqueado/liberado: el cursor se muestra y se puede hacer click en los menús.
    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        _waitingForCaptureClickRelease = true;
        CanUseHandObjects = false;
    }

    #endregion
}
