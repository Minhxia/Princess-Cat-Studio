using UnityEngine;
using UnityEngine.SceneManagement;

// Clase que gestiona el final de partida (victoria, derrota... y lágrimas)
public class GameEndController : MonoBehaviour
{
    [Header("GameOver scene")]
    [SerializeField] private string _gameOverSceneName = "GameOver";

    private bool _hasEnded; // indica si el juego ha terminado

    // Comprueba si es posible cambiar a la pantalla de GameOver antes de salvar 
    // al último niño
    public bool CanEndGame()
    {
        if (!Application.isPlaying || _hasEnded || !isActiveAndEnabled)
            return false;
        
        // se comprueba si se puede cargar la escena de gameover
        if (string.IsNullOrWhiteSpace(_gameOverSceneName)
            || !Application.CanStreamedLevelBeLoaded(_gameOverSceneName))
        {
            Debug.LogError("[GameEndController] Hay que añadir la escena de GameOver al Build Profile: "
                + _gameOverSceneName, this);
            return false;
        }

        return true;
    }

    // Termina la partida
    private void EndGame(bool hasWon)
    {
        // se comprueba si se puede cargar la escena
        if (!CanEndGame()) return;

        // se indica el fin D:
        _hasEnded = true;
        // y se envía la información necesaria
        GameResultData.SetResult(hasWon, SceneManager.GetActiveScene().name);

        // por si acaso, se restablece el estado antes de ir a la escena de GameOver
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene(_gameOverSceneName);
    }

    // TODO: borrar métodos para debug?
    [ContextMenu("Try win game")]
    public void Win()
    {
        EndGame(true);
    }
    [ContextMenu("Try lose game")]
    public void Lose()
    {
        EndGame(false);
    }
}
