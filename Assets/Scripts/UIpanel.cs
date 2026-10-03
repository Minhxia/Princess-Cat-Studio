using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HorrorGame.UI
{
    /// <summary>
    /// Base class for every UI screen (pause, settings, inventory, death...).
    /// Visibility is controlled with a CanvasGroup instead of SetActive, so panels
    /// can fade in/out and their scripts keep running (and keep their event subscriptions).
    /// Panels are opened/closed through UIManager, not directly.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UIPanel : MonoBehaviour
    {
        [Header("Behaviour")]
        [Tooltip("Sets Time.timeScale to 0 while this panel is open.")]
        [SerializeField] private bool pausesGame = true;

        [Tooltip("Unlocks and shows the mouse cursor while this panel is open.")]
        [SerializeField] private bool showsCursor = true;

        [Header("Animation")]
        [Tooltip("Fade time in seconds. Uses unscaled time, so it works while the game is paused.")]
        [SerializeField, Min(0f)] private float fadeDuration = 0.2f;

        [Header("Navigation")]
        [Tooltip("Element selected when the panel opens (needed for keyboard/gamepad navigation).")]
        [SerializeField] private GameObject firstSelected;

        private CanvasGroup canvasGroup;
        private Coroutine fadeRoutine;

        public bool IsVisible { get; private set; }
        public bool PausesGame => pausesGame;
        public bool ShowsCursor => showsCursor;

        protected virtual void Awake()
        {
            // Every panel starts hidden. UIManager decides what is shown.
            SetVisibility(false, instant: true);
        }

        /// <summary>Called by UIManager. Do not call directly from buttons.</summary>
        public void Show()
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            SetVisibility(true, instant: false);
            SelectFirstElement();
            OnShown();
        }

        /// <summary>Called by UIManager. Do not call directly from buttons.</summary>
        public void Hide()
        {
            SetVisibility(false, instant: false);
            OnHidden();
        }

        // Hooks for child classes (Template Method pattern).
        // Example: InventoryUI overrides OnShown() to refresh its slots.
        protected virtual void OnShown() { }
        protected virtual void OnHidden() { }

        private void SetVisibility(bool visible, bool instant)
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

            IsVisible = visible;
            canvasGroup.interactable = visible;   // buttons can be clicked
            canvasGroup.blocksRaycasts = visible; // mouse clicks don't pass through

            float target = visible ? 1f : 0f;

            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }

            if (instant || fadeDuration <= 0f || !isActiveAndEnabled)
                canvasGroup.alpha = target;
            else
                fadeRoutine = StartCoroutine(Fade(target));
        }

        private IEnumerator Fade(float target)
        {
            float start = canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                // unscaledDeltaTime: keeps working when Time.timeScale = 0 (paused)
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(start, target, elapsed / fadeDuration);
                yield return null;
            }

            canvasGroup.alpha = target;
            fadeRoutine = null;
        }

        private void SelectFirstElement()
        {
            if (EventSystem.current == null) return;

            // Clearing first forces the selection highlight to refresh.
            EventSystem.current.SetSelectedGameObject(null);
            if (firstSelected != null)
                EventSystem.current.SetSelectedGameObject(firstSelected);
        }
    }
}