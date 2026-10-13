using System;
using UnityEngine;

namespace HorrorGame.Achievements
{
    /// <summary>
    /// Saves and reads which achievements are unlocked (PlayerPrefs).
    /// Static class: no GameObject needed, usable from any scene.
    ///
    /// From gameplay code, unlocking is one line:
    ///     AchievementSystem.Unlock("first_note");
    /// </summary>
    public static class AchievementSystem
    {
        private const string KeyPrefix = "achievement_";

        /// <summary>Raised with the id when an achievement is unlocked for the first time
        /// (useful later for an "Achievement unlocked!" popup in game).</summary>
        public static event Action<string> AchievementUnlocked;

        public static bool IsUnlocked(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            return PlayerPrefs.GetInt(KeyPrefix + id, 0) == 1;
        }

        /// <summary>Unlocks and saves. Returns true only the first time.</summary>
        public static bool Unlock(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning("[Achievements] Unlock called with an empty id.");
                return false;
            }
            if (IsUnlocked(id)) return false;

            PlayerPrefs.SetInt(KeyPrefix + id, 1);
            PlayerPrefs.Save(); // write to disk now, so a crash doesn't lose it
            AchievementUnlocked?.Invoke(id);
            return true;
        }

        /// <summary>Locks it again. Intended for testing only.</summary>
        public static void Lock(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            PlayerPrefs.DeleteKey(KeyPrefix + id);
            PlayerPrefs.Save();
        }

        // Static events survive between Play sessions when "Enter Play Mode Options"
        // disables domain reload. This clears old subscribers every time Play starts.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            AchievementUnlocked = null;
        }
    }
}
