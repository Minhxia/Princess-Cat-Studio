using System;
using HorrorGame.Achievements;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HorrorGame.UI
{
    /// <summary>
    /// ONE badge in the achievements grid. Only displays data (the "View").
    /// Reports hover (mouse) and selection (keyboard/gamepad) to the panel,
    /// which shows the description.
    /// </summary>
    public class AchievementSlotUI : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        [Header("References (children of the prefab)")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;

        [Header("Look")]
        [Tooltip("Icon color while locked.")]
        [SerializeField] private Color lockedTint = new Color(0.25f, 0.25f, 0.25f, 1f);

        [Tooltip("Text shown instead of the name for secret achievements still locked.")]
        [SerializeField] private string secretName = "???";

        private AchievementData data;
        private Action<AchievementData> onFocused;
        private Action onUnfocused;

        public AchievementData Data => data;
        public bool IsUnlocked { get; private set; }

        /// <summary>Called once by AchievementsPanel after creating the slot.</summary>
        public void Init(AchievementData achievement, Action<AchievementData> focused, Action unfocused)
        {
            data = achievement;
            onFocused = focused;
            onUnfocused = unfocused;
            gameObject.name = $"Slot_{achievement.Id}"; // readable names in the Hierarchy while debugging
            Refresh();
        }

        /// <summary>Re-reads the saved state and updates the visuals.</summary>
        public void Refresh()
        {
            if (data == null) return;

            IsUnlocked = AchievementSystem.IsUnlocked(data.Id);

            if (iconImage != null)
            {
                iconImage.sprite = data.Icon; // null = plain square (placeholder)
                iconImage.color = IsUnlocked ? Color.white : lockedTint;
            }

            if (nameText != null)
                nameText.text = (data.IsSecret && !IsUnlocked) ? secretName : data.Title;
        }

        // Mouse
        public void OnPointerEnter(PointerEventData eventData) => onFocused?.Invoke(data);
        public void OnPointerExit(PointerEventData eventData) => onUnfocused?.Invoke();

        // Keyboard / gamepad (requires a Selectable, e.g. a Button, on this object)
        public void OnSelect(BaseEventData eventData) => onFocused?.Invoke(data);
        public void OnDeselect(BaseEventData eventData) => onUnfocused?.Invoke();
    }
}
