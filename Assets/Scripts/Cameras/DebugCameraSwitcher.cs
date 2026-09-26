using UnityEngine;
using UnityEngine.InputSystem;

// Clase auxiliar que permite cambiar entre la cámara de depuración y la cámara en primera persona
// TODO: es solo para depurar, quizás se borre más adelante
public class DebugCameraSwitcher : MonoBehaviour
{
    #region Variables
    [SerializeField] private Camera _firstPersonaCamera;
    [SerializeField] private Camera _debugCamera;
    [SerializeField] private Key switchCameraKey = Key.F3;

    private bool _isDebugCameraActive;
    #endregion

    #region Unity Methods
    private void Awake()
    {
        ActivateDebugCamera(false);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard != null && keyboard[switchCameraKey].wasPressedThisFrame)
        {
            ActivateDebugCamera(!_isDebugCameraActive);
        }
    }
    #endregion

    // Activa o desactiva la cámara de depuración
    private void ActivateDebugCamera(bool activated)
    {
        _isDebugCameraActive = activated;

        _firstPersonaCamera.gameObject.SetActive(!activated);
        _debugCamera.gameObject.SetActive(activated);
    }
}
