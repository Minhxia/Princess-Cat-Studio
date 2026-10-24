using UnityEngine;
using UnityEngine.SceneManagement;

// Clase que gestiona los menús de la interfaz
public class MainMenuController : MonoBehaviour
{
    [Header("Game scene")]
    [SerializeField] private string gameplaySceneName = "Game";

    [Header("Canvas panels")]
    [SerializeField] private GameObject _mainMenuPanel;
    [SerializeField] private GameObject _settingsPanel;
    [SerializeField] private GameObject _achievementsPanel;
    [SerializeField] private GameObject _creditsPanel;

    private void Awake()
    {
        // se libera y se muestra el cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // en caso de volver al menú principal desde el menú de pausa del juego,
        // se resetea el tiempo a su velocidad normal
        Time.timeScale = 1f;

        if (_mainMenuPanel == null || _settingsPanel == null ||
            _achievementsPanel == null || _creditsPanel == null)
        {
            Debug.LogError("[MainMenuController] Asigna todos los paneles en el Inspector.", this);
            return;
        }

        ShowMainMenu();
    }

    // Jugar
    public void Play()
    {
        if (string.IsNullOrWhiteSpace(gameplaySceneName) ||
            !Application.CanStreamedLevelBeLoaded(gameplaySceneName))
        {
            Debug.LogError("[MainMenuController] La escena del juego no existe o no esta incluida en la lista de escenas de la build.", this);
            return;
        }

        SceneManager.LoadScene(gameplaySceneName);
    }

    // Salir
    public void Exit()
    {
        // para salir del juego desde el editor
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        // y desde la web
        #else
            Application.Quit();
        #endif
    }

    // Abre los enlaces a las cuentas de nuestras redes sociales
    public void OpenSocialLink(string url)
    {
        Application.OpenURL(url);
    }


    // Muestra un panel (uno de los menús) y oculta el resto
    private void ShowPanel(GameObject selectedPanel)
    {
        if (selectedPanel == null)
        {
            Debug.LogError("[MainMenuController] Falta asignar el panel solicitado.", this);
            return;
        }

        _mainMenuPanel.SetActive(selectedPanel == _mainMenuPanel);
        _settingsPanel.SetActive(selectedPanel == _settingsPanel);
        _achievementsPanel.SetActive(selectedPanel == _achievementsPanel);
        _creditsPanel.SetActive(selectedPanel == _creditsPanel);
    }

    #region Navigation Methods
    public void ShowMainMenu() => ShowPanel(_mainMenuPanel);
    public void ShowSettings() => ShowPanel(_settingsPanel);
    public void ShowAchievements() => ShowPanel(_achievementsPanel);
    public void ShowCredits() => ShowPanel(_creditsPanel);
    #endregion
}
