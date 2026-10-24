using UnityEngine;
using UnityEngine.SceneManagement;

// Clase que gestiona el final de partida (victoria, derrota)
public class GameEndController : MonoBehaviour
{
    [Header("GameOver scene name")]
    [SerializeField] private string _gameOverSceneName = "GameOver";

    private bool _hasEnded;

    private void EndGame(bool hasWon)
    {
        if (!Application.isPlaying || _hasEnded) return;

        if(!Application.CanStreamedLevelBeLoaded(_gameOverSceneName))
        {
            Debug.LogError("[GameEndController] Tienes que añadir la escena de GameOver al Build Profile: " 
                + _gameOverSceneName, this);
            return;
        }

        _hasEnded = true;

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
