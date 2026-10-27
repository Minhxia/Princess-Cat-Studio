using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]

// Clase que consulta si su logro está bloqueado o desbloqueado, y muestra el sprite correspondiente
public class AchievementBadgeUI : MonoBehaviour
{
    [Header("ID")]
    [SerializeField] private string _achievementId;

    [Header("Images")]
    [SerializeField] private Image _badgeImage; 
    [SerializeField] private Sprite _lockedSprite;
    [SerializeField] private Sprite _unlockedSprite;
    

    // Suscripción al evento de cambio de logro al activarse
    private void OnEnable()
    {
        AchievementProgress.Changed += OnAchievementChanged;
        // se actualiza el estado del logro
        Refresh();
    }

    // Desuscripción al evento de cambio de logro al desactivarse
    private void OnDisable()
    {
        AchievementProgress.Changed -= OnAchievementChanged;
    }

    // Evento de cambio de logro
    private void OnAchievementChanged (string changeId)
    {
        // el propio logro comprueba si "él" es el que acaba de cambiar, ya que si no es así,
        // no necesita actualizarse
        if (changeId == _achievementId)
        {
            Refresh();
        }
    }

    // Actualiza el logro
    private void Refresh()
    {
        // se comprueba si el logro está desbloqueado y si es necesario, se actualiza
        // su sprite (bloqueado o desbloqueado)
        bool unlocked = AchievementProgress.IsUnlocked(_achievementId);
        _badgeImage.sprite = unlocked ? _unlockedSprite : _lockedSprite;
    }

    // TODO: borrar métodos para debug?
    [ContextMenu("Try unlock")]
    private void TestUnlock()
    {
        if (Application.isPlaying)
        {
            AchievementProgress.Unlock(_achievementId);
        }
    }

    [ContextMenu("Lock again")]
    private void TestReset()
    {
        if (Application.isPlaying)
        {
            AchievementProgress.ResetAchievement(_achievementId);
        }
    }

}
