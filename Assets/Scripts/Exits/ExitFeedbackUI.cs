using UnityEngine;
using System.Collections;
using TMPro;

// Clase del panel que muestra el feedback relacionado con las puertas de emergencia y el rescate.
public class ExitFeedbackUI : MonoBehaviour
{
    [Header("Exit message")]
    [SerializeField] private GameObject _exitsFeedbackPanel;
    [SerializeField] private TMP_Text _messageText;
    [SerializeField, Min(0.1f)] private float _messageSeconds = 2.5f;

    [Header("Rescue prompt")]
    [SerializeField] private GameObject _rescueFeedbackPanel;
    [SerializeField] private TMP_Text _rescuePromptText;

    private Coroutine _messageRoutine;

    private void Awake()
    {
        if (_exitsFeedbackPanel != null)
            _exitsFeedbackPanel.SetActive(false);

        if (_rescueFeedbackPanel != null)
            _rescueFeedbackPanel.SetActive(false);
    }

    // Muestra el feedback del desbloqueo de las puertas de emergencia
    public void ShowExitUnlocked()
    {
        ShowTemporaryMessage("Emergency exit unlocked.");
    }

    // Muestra el feedback del rescate de los niños
    public void ShowRescueProgress(int savedNow, int total, int required)
    {
        string message = "Saved " + savedNow + " child";

        // si se salva más de uno debe poner: children
        if (savedNow != 1)
            message += "ren";

        message += ". " + total + "/" + required + " children safe";

        ShowTemporaryMessage(message);
    }

    // Muestra el mensaje al jugador de que puede interactuar con esa zona para
    // salvar a los niños
    public void SetRescuePrompt(bool visible)
    {
        if (_rescueFeedbackPanel == null || _rescuePromptText == null)
            return;

        _rescuePromptText.text = "[F] Save child";
        _rescueFeedbackPanel.SetActive(visible);
    }

    // Muestra el feedback correspondiente al jugador
    private void ShowTemporaryMessage(string message)
    {
        if (_exitsFeedbackPanel == null || _messageText == null)
            return;

        if (_messageRoutine != null)
            StopCoroutine(_messageRoutine);

        _messageText.text = message;
        _exitsFeedbackPanel.SetActive(true);

        _messageRoutine = StartCoroutine(HideMessageAfterDelay());
    }

    // Corrutina para ocultar el feedback (se muestra sólo unos segundos)
    private IEnumerator HideMessageAfterDelay()
    {
        yield return new WaitForSecondsRealtime(_messageSeconds);

        _exitsFeedbackPanel.SetActive(false);
        _messageRoutine = null;
    }
}