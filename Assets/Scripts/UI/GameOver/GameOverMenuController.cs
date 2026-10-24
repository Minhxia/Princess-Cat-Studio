using UnityEngine;
using UnityEngine.SceneManagement;

// Clase que gestiona la escena de final de partida (animación de victoria o derrota)
public class GameOverMenuController : MonoBehaviour
{
    [Header("Canvas panels")]    
    [SerializeField] private GameObject _victoryPanel;
    [SerializeField] private GameObject _defeatPanel;

    [Header("Main menu scene")]
    [SerializeField] private string _mainMenuSceneName = "MainMenu";

    private bool _isChangingScene;

    private void Start()
    {
        if ( _victoryPanel == null || _defeatPanel == null)
        {
            Debug.LogError("[GameOverMenuController] Tienes que asignar los paneles de victoria y derrota.", this);
            return;
        }

        _victoryPanel.SetActive(GameResultData.HasWon);
        _defeatPanel.SetActive(!GameResultData.HasWon);
    }

    // Carga una escena
    private void LoadScene(string sceneName)
    {
        if (_isChangingScene) return;

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError("[GameOverMenuController] La escena no está disponible en el Build Profile: " +
                sceneName, this);
            return;
        }

        _isChangingScene = true;
        SceneManager.LoadScene(sceneName);
    }

    // Volver a jugar
    public void PlayAgain()
    {
        LoadScene(GameResultData.GameplaySceneName);
    }

    // Volver al menú principal
    public void ReturnToMainMenu()
    {
        LoadScene(_mainMenuSceneName);
    }
}
