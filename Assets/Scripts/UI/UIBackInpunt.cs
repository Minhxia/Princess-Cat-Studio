using UnityEngine;
using UnityEngine.InputSystem;

namespace HorrorGame.UI
{
    /// <summary>
    /// Handles the Back key (ESC / gamepad Start) for the whole scene:
    ///  - If a menu is open  -> closes the top one (same as a VOLVER button).
    ///  - If nothing is open -> opens 'Open When Nothing Open' (the Pause panel in game).
    /// Put it on the UIManager object. One per scene.
    /// </summary>
    public class UIBackInput : MonoBehaviour
    {
        [Tooltip("Keys that act as Back. You can add or change bindings here in the Inspector.")]
        [SerializeField] private InputAction backAction = CreateDefaultBackAction();

        [Tooltip("Panel opened when Back is pressed and no menu is open. " +
                 "Gameplay scene: Panel_Pausa. Main menu: leave EMPTY.")]
        [SerializeField] private UIPanel openWhenNothingOpen;

        private static InputAction CreateDefaultBackAction()
        {
            var action = new InputAction("Back", InputActionType.Button);
            action.AddBinding("<Keyboard>/escape");
            action.AddBinding("<Gamepad>/start");
            return action;
        }

        // An InputAction does nothing until it is enabled.
        // We enable/disable it together with this component, and subscribe/unsubscribe
        // in pairs, so no callbacks are left behind when the scene changes.
        private void OnEnable()
        {
            backAction.performed += OnBack;
            backAction.Enable();
        }

        private void OnDisable()
        {
            backAction.performed -= OnBack;
            backAction.Disable();
        }

        private void OnBack(InputAction.CallbackContext context)
        {
            UIManager ui = UIManager.Instance;
            if (ui == null) return;

            if (ui.IsAnyMenuOpen)
            {
                // Some screens (Game Over) must be left with their own buttons.
                if (ui.TopPanel != null && !ui.TopPanel.ClosesWithBack) return;
                ui.CloseTopPanel();
            }
            else if (openWhenNothingOpen != null)
            {
                ui.OpenPanel(openWhenNothingOpen);
            }
        }
    }
}