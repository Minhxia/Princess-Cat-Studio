using System;
using System.Collections.Generic;
using UnityEngine;

namespace HorrorGame.UI
{
    /// <summary>
    /// Opens and closes UIPanels using a stack, so "Back" always returns to the
    /// previous screen (Pause -> Settings -> Back -> Pause).
    /// Also owns the cursor state and Time.timeScale while menus are open.
    /// One per scene (NOT DontDestroyOnLoad).
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Tooltip("Panel always open at the bottom of the stack (e.g. Main Menu). Leave EMPTY in gameplay scenes.")]
        [SerializeField] private UIPanel rootPanel;

        [Tooltip("TRUE in gameplay scenes: cursor locked when no menu is open. FALSE in menu/sandbox scenes.")]
        [SerializeField] private bool lockCursorWhenNoPanelOpen = true;

        private readonly Stack<UIPanel> panelStack = new Stack<UIPanel>();

        /// <summary>True if any panel other than the root is open.</summary>
        public bool IsAnyMenuOpen => panelStack.Count > (rootPanel != null ? 1 : 0);

        public UIPanel TopPanel => panelStack.Count > 0 ? panelStack.Peek() : null;

        /// <summary>
        /// Raised when the first menu opens (true) or the last one closes (false).
        /// The player controller will listen to this to disable movement/look.
        /// </summary>
        public event Action<bool> MenuStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Duplicate UIManager found in scene. Destroying this one.", this);
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Runs after every UIPanel.Awake() has hidden itself.
            if (rootPanel != null)
            {
                panelStack.Push(rootPanel);
                rootPanel.Show();
            }
            ApplyState();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                // Safety: never leave the next scene frozen if we load a scene while paused.
                Time.timeScale = 1f;
            }
        }

        /// <summary>Opens a panel on top of the current one. Usable from Button OnClick.</summary>
        public void OpenPanel(UIPanel panel)
        {
            if (panel == null || panelStack.Contains(panel)) return;

            bool menuWasOpen = IsAnyMenuOpen;

            if (panelStack.Count > 0) panelStack.Peek().Hide();
            panelStack.Push(panel);
            panel.Show();

            ApplyState();
            if (!menuWasOpen) MenuStateChanged?.Invoke(true);
        }

        /// <summary>Closes the top panel and re-shows the previous one ("Back"). Usable from Button OnClick.</summary>
        public void CloseTopPanel()
        {
            if (panelStack.Count == 0) return;
            if (panelStack.Peek() == rootPanel) return; // the root (main menu) can't be closed

            panelStack.Pop().Hide();
            if (panelStack.Count > 0) panelStack.Peek().Show();

            ApplyState();
            if (!IsAnyMenuOpen) MenuStateChanged?.Invoke(false);
        }

        /// <summary>Closes everything except the root panel (e.g. "Resume" button).</summary>
        public void CloseAllPanels()
        {
            if (!IsAnyMenuOpen) return;

            while (panelStack.Count > 0 && panelStack.Peek() != rootPanel)
                panelStack.Pop().Hide();

            if (panelStack.Count > 0) panelStack.Peek().Show();

            ApplyState();
            MenuStateChanged?.Invoke(false);
        }

        private void ApplyState()
        {
            bool pause = false;
            bool showCursor = !lockCursorWhenNoPanelOpen;

            // If ANY open panel wants pause/cursor, we apply it.
            foreach (UIPanel panel in panelStack)
            {
                pause |= panel.PausesGame;
                showCursor |= panel.ShowsCursor;
            }

            Time.timeScale = pause ? 0f : 1f;
            Cursor.lockState = showCursor ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = showCursor;
        }
    }
}