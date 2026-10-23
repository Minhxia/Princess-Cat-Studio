using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]

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
    private float _previousTimeScale = 1f;
    private bool _previousAudioPause; // se guarda si el audio estaba pausado o no
    private bool _cursorWasLocked;
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
    #endregion

    public void Pause()
    {
        
    }

    public void Resume()
    {
        
    }

    public void BackToPause()
    {
        
    }
}
