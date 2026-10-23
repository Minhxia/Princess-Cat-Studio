using System;
using UnityEngine;

// Clase que gestiona el estado de los logros (se usa PlayerPrefs).
// static -> para poder utilizar la clase directamente mediante su nombre, sin crear un objeto
// ni tener que añadirla como componente.
public static class AchievementProgress
{
    private const string KeyPrefix = "Achievement_";    // prefijo de la clave en PlayerPrefs

    public static event Action<string> Changed; // cuando se produce un cambio en un logro (actualización)

    // Indica si un logro está desbloqueado o no
    public static bool IsUnlocked(string achievementId)
    {
        if (string.IsNullOrWhiteSpace(achievementId)) return false;

        // si el valor KeyPrefix + achievementId existe, devuelve 1, si no, devuelve 0
        // (ya que 1 significa desbloqueado)
        return PlayerPrefs.GetInt(KeyPrefix + achievementId, 0) == 1;
    }

    // Desbloquea el logro indicado
    public static void Unlock(string achievementId)
    {
        if (string.IsNullOrWhiteSpace(achievementId)) return;
        if (IsUnlocked(achievementId)) return;

        PlayerPrefs.SetInt(KeyPrefix + achievementId, 1);
        PlayerPrefs.Save();
        Debug.Log("[AchievementProgress] El logro " + KeyPrefix + achievementId + " se ha desbloqueado!");

        // se avisa de que el logro ha cambiado
        Changed?.Invoke(achievementId);
    }

    // Vuelve a bloquear un logro
    public static void ResetAchievement(string achievementId)
    {
        if (string.IsNullOrWhiteSpace(achievementId)) return;

        PlayerPrefs.DeleteKey(KeyPrefix + achievementId);
        PlayerPrefs.Save();
        Debug.Log("[AchievementProgress] El logro " + KeyPrefix + achievementId + " se ha reseteado.");

        // se avisa de que el logro ha cambiado
        Changed?.Invoke(achievementId);
    }
}
