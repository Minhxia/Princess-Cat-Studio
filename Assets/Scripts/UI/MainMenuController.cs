using UnityEngine;
using UnityEngine.SceneManagement;

namespace HorrorGame.UI
{
    /// <summary>
    /// Actions of the main menu buttons that are NOT about opening panels
    /// (opening/closing panels is UIManager's job).
    /// JUGAR -> PlayGame(), SALIR -> QuitGame().
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Tooltip("Exact name of the scene loaded by JUGAR (case-sensitive). " +
                 "It must be added in File > Build Profiles > Scene List.")]
        [SerializeField] private string gameSceneName = "LevelX";

        /// <summary>Loads the game scene. Called by Btn_Jugar.</summary>
        public void PlayGame()
        {
            // Fail with a clear message instead of Unity's generic error
            // if the name is wrong or the scene is missing from the Scene List.
            if (!Application.CanStreamedLevelBeLoaded(gameSceneName))
            {
                Debug.LogError($"[MainMenu] Scene '{gameSceneName}' can't be loaded. " +
                               "Check the spelling and that it is in File > Build Profiles > Scene List.", this);
                return;
            }

            SceneManager.LoadScene(gameSceneName);
        }

        /// <summary>Closes the game. Called by Btn_Salir.</summary>
        public void QuitGame()
        {
#if UNITY_EDITOR
            // Application.Quit() is ignored inside the editor,
            // so in the editor we stop Play mode instead.
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}