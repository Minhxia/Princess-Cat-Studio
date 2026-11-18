using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Orden de ejecución del script:
[DefaultExecutionOrder(-3)] // antes de la cámara, que es -1

// Clase que gestiona la pausa de la partida
public class PauseMenuManager : MonoBehaviour
{
    #region Variables
    [Header("Canvas panels")]
    [SerializeField] private GameObject _pausePanel;
    [SerializeField] private GameObject _settingsPanel;

    [Header("Player camera")]
    [SerializeField] private FirstPersonCameraController _cameraController;

    [Header("Input scripts")]
    [SerializeField] private MonoBehaviour[] _gameplayInputScripts = new MonoBehaviour[0];

    [Header("Main menu scene")]
    [SerializeField] private string _mainMenuSceneName = "MainMenu";

    private readonly List<MonoBehaviour> _disabledInputScripts = new List<MonoBehaviour>();
    private float _previousTimeScale = 1f; // guarda la velocidad del tiempo antes de pausar
    private bool _previousAudioPause;      // guarda el estado del audio antes de pausar
    private bool _cursorWasLocked;         // guarda si el cursor estaba bloqueado en la comprobación anterior
    #endregion

    public bool IsPaused { get; private set; } // para indicar si la pausa está activa

    #region Unity Methods
    private void Awake()
    {
        if (_pausePanel == null || _settingsPanel == null || _cameraController == null)
        {
            Debug.LogError("[PauseMenuController] Tienes que asignar los paneles y la cámara!", this);
            enabled = false;    // se desactiva el comportamiento/script
            return;
        }

        _pausePanel.SetActive(false);
        _settingsPanel.SetActive(false);
    }

    private void Update()
    {
        // indica si el cursor está bloqueado ahora
        bool cursorLocked = Cursor.lockState == CursorLockMode.Locked;
        // indica si el cursor estaba bloqueado antes y si ahora está libre, es decir, se
        // comprueba si se acaba de liberar ahora
        bool cursorJustReleased = _cursorWasLocked && !cursorLocked;
        _cursorWasLocked = cursorLocked;

        Keyboard keyboard = Keyboard.current;
        // se comprueba ESC, que abre el menú de pausa
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            // si está jugando, se pausa
            if (!IsPaused)
            {
                Pause();
            }
            // si está en el menú de ajustes, vuelve al menú de pausa
            else if (_settingsPanel.activeSelf)
            {
                BackToPause();
            }
            // si está en el menú de pausa, y reanuda la partida
            else
            {
                Resume();
            }
            return;
        }

        // en caso de que se detecte que se ha liberado el cursor, aunque no se haya recibido
        // la pulsación de ESC, también se pausa el juego (por WebGL)
        if (!IsPaused && cursorJustReleased)
        {
            Pause();
        }
    }

    private void OnDestroy()
    {
        if (!IsPaused) return;

        Time.timeScale = _previousTimeScale;
        AudioListener.pause = _previousAudioPause;
    }
    #endregion

    #region Public Methods
    // Pausa la partida
    public void Pause()
    {
        if (IsPaused) return;

        // se guarda el estado previo
        _previousTimeScale = Time.timeScale;
        _previousAudioPause = AudioListener.pause;
        
        // se pausa como tal el juego, se desactivan los controles
        IsPaused = true;
        _cameraController.SetPaused(true);
        DisableGameplayInput();

        Time.timeScale = 0f;
        AudioListener.pause = true;

        _pausePanel.SetActive(true);
    }

    // Reanuda la partida
    public void Resume()
    {
        if (!IsPaused) return;

        // se desactivan los menús
        _pausePanel.SetActive(false);
        _settingsPanel.SetActive(false);

        // se restablece el estado anterior a la pausa
        IsPaused = false;
        Time.timeScale = _previousTimeScale;
        AudioListener.pause = _previousAudioPause;
        _cursorWasLocked = false;

        // se vuelven a activar los controles
        _cameraController.SetPaused(false);
        RestoreGameplayInput();
    }

    // Abre el menú de ajustes desde el menú de pausa
    public void OpenSettings()
    {
        if (!IsPaused) return;

        _pausePanel.SetActive(false);
        _settingsPanel.SetActive(true);
    }

    // Vuelve al menú de pausa desde el menú de ajustes
    public void BackToPause()
    {
        if (!IsPaused) return;

        _settingsPanel.SetActive(false);        
        _pausePanel.SetActive(true);
    }

    // Vuelve al menú principal desde el menú de pausa
    public void ReturnToMainMenu()
    {
        // primero se comprueba si se puede cargar la escena
        if (!Application.CanStreamedLevelBeLoaded(_mainMenuSceneName))
        {
            Debug.LogError("[PauseMenuController] La escena del menú principal no está disponible " +
                "en el Build Profile: " + _mainMenuSceneName, this);
            return;
        }

        // se resetean los valores
        IsPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // se carga la escena
        SceneManager.LoadScene(_mainMenuSceneName);
    }
    #endregion

    #region Private Methods
    // Desactiva los componentes con inputs al abrir el menú de pausa
    private void DisableGameplayInput()
    {
        _disabledInputScripts.Clear();

        foreach (MonoBehaviour inputScript in _gameplayInputScripts)
        {
            if (inputScript == null || inputScript == this || inputScript == _cameraController
                    || !inputScript.enabled)
            {
                continue;
            }

            _disabledInputScripts.Add(inputScript);
            inputScript.enabled = false;
        }

        Debug.Log("[PauseMenuController] Scripts con inputs desactivados.");
    }

    // Restablece los componentes con inputs al cerrar el menú de pausa
    private void RestoreGameplayInput()
    {
        foreach (MonoBehaviour inputScript in _disabledInputScripts)
        {
            if (inputScript != null)
            {
                inputScript.enabled = true;
            }
        }
        _disabledInputScripts.Clear();

        Debug.Log("[PauseMenuController] Scripts con inputs activados de nuevo.");
    }
    #endregion
}
