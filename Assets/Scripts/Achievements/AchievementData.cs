using UnityEngine;

namespace HorrorGame.Achievements
{
    /// <summary>
    /// Definition of ONE achievement, stored as an asset in the project.
    /// Create with: right-click in Project > Create > Horror Game > Achievement.
    /// Adding a new achievement = creating a new asset. No code, no scene changes.
    /// </summary>
    [CreateAssetMenu(fileName = "ACH_New", menuName = "Horror Game/Achievement")]
    public class AchievementData : ScriptableObject
    {
        [Tooltip("Unique key used to save progress and to unlock it from code. " +
                 "Lowercase, no spaces (e.g. 'first_note'). Don't change it once the game is shipped, " +
                 "or players lose that achievement.")]
        [SerializeField] private string id;

        [Tooltip("Name shown on the badge.")]
        [SerializeField] private string title;

        [Tooltip("Shown at the bottom of the screen when the badge is hovered or selected.")]
        [SerializeField, TextArea(2, 4)] private string description;

        [Tooltip("Badge image. If empty, a plain square is shown (placeholder).")]
        [SerializeField] private Sprite icon;

        [Tooltip("If ticked, name and description are hidden ('???') until it is unlocked.")]
        [SerializeField] private bool isSecret;

        // Read-only properties: other scripts can read the data but never modify the asset.
        public string Id => id;
        public string Title => title;
        public string Description => description;
        public Sprite Icon => icon;
        public bool IsSecret => isSecret;
    }
}
