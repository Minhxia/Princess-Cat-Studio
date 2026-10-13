using System.Collections.Generic;
using HorrorGame.Achievements;
using TMPro;
using UnityEngine;

namespace HorrorGame.UI
{
    /// <summary>
    /// The LOGROS screen. It IS a UIPanel (inheritance), so UIManager opens/closes it
    /// like any other panel. It creates one badge per AchievementData and
    /// refreshes them every time the panel is shown.
    /// </summary>
    public class AchievementsPanel : UIPanel
    {
        [Header("Achievements")]
        [Tooltip("All achievement assets, in display order.")]
        [SerializeField] private List<AchievementData> achievements = new List<AchievementData>();

        [Tooltip("The UI_AchievementSlot prefab (from the PROJECT window).")]
        [SerializeField] private AchievementSlotUI slotPrefab;

        [Tooltip("Object with the Grid Layout Group (from the HIERARCHY).")]
        [SerializeField] private Transform slotContainer;

        [Tooltip("Text where the hovered/selected achievement is described.")]
        [SerializeField] private TMP_Text descriptionText;

        [SerializeField, TextArea(1, 3)]
        private string defaultDescription = "Selecciona una insignia para ver su descripción.";

        private readonly List<AchievementSlotUI> slots = new List<AchievementSlotUI>();

        protected override void Awake()
        {
            base.Awake(); // IMPORTANT: runs UIPanel.Awake, which hides the panel
            BuildSlots();
        }

        // Hook from UIPanel: called every time UIManager shows this panel.
        protected override void OnShown()
        {
            RefreshAll();
            ShowDefaultDescription();
        }

        private void BuildSlots()
        {
            if (slotPrefab == null || slotContainer == null)
            {
                Debug.LogError("[AchievementsPanel] Assign 'Slot Prefab' and 'Slot Container' in the Inspector.", this);
                return;
            }

            // Remove any preview badges left in the grid while designing in the editor.
            for (int i = slotContainer.childCount - 1; i >= 0; i--)
                Destroy(slotContainer.GetChild(i).gameObject);

            var usedIds = new HashSet<string>();

            foreach (AchievementData achievement in achievements)
            {
                if (achievement == null)
                {
                    Debug.LogWarning("[AchievementsPanel] Empty element in the Achievements list.", this);
                    continue;
                }
                if (string.IsNullOrEmpty(achievement.Id) || !usedIds.Add(achievement.Id))
                {
                    // Two assets with the same id would share their saved state.
                    Debug.LogError($"[AchievementsPanel] '{achievement.name}' has an empty or DUPLICATED id '{achievement.Id}'.", achievement);
                    continue;
                }

                AchievementSlotUI slot = Instantiate(slotPrefab, slotContainer);
                slot.Init(achievement, ShowDescription, ShowDefaultDescription);
                slots.Add(slot);
            }
        }

        private void RefreshAll()
        {
            foreach (AchievementSlotUI slot in slots)
                slot.Refresh();
        }

        private void ShowDescription(AchievementData achievement)
        {
            if (descriptionText == null) return;

            bool unlocked = AchievementSystem.IsUnlocked(achievement.Id);

            if (achievement.IsSecret && !unlocked)
                descriptionText.text = "<b>???</b>\nLogro secreto.";
            else if (unlocked)
                descriptionText.text = $"<b>{achievement.Title}</b>\n{achievement.Description}";
            else
                descriptionText.text = $"<b>{achievement.Title}</b> (bloqueado)\n{achievement.Description}";
        }

        private void ShowDefaultDescription()
        {
            if (descriptionText != null) descriptionText.text = defaultDescription;
        }

        // ---------- Testing helpers ----------
        // In Play mode: click the ⋮ on this component in the Inspector and choose them.

        [ContextMenu("Debug/Unlock All Achievements")]
        private void DebugUnlockAll()
        {
            foreach (AchievementData a in achievements)
                if (a != null) AchievementSystem.Unlock(a.Id);
            RefreshAll();
        }

        [ContextMenu("Debug/Lock All Achievements")]
        private void DebugLockAll()
        {
            foreach (AchievementData a in achievements)
                if (a != null) AchievementSystem.Lock(a.Id);
            RefreshAll();
        }
    }
}
