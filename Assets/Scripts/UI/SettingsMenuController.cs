using UnityEngine;
using UnityEngine.UI;

// Clase auxiliar que gestiona los submenús del menú de ajustes
public class SettingsMenuController : MonoBehaviour
{
    [Header("Canvas panels")]
    [SerializeField] private GameObject _audioPanel;
    [SerializeField] private GameObject _controlsPanel;

    [Header("Tab buttons")]
    [SerializeField] private Button _audioButton;
    [SerializeField] private Button _controlsButton;

    // Al entrar al menú de ajustes, por defecto se abren los ajustes de audio
    private void Awake()
    {
        ShowAudio();
    }

    // Muestra el contenido correspondiente del menú de ajustes
    private void ShowTab(bool showAudio)
    {
        _audioPanel.SetActive(showAudio);
        _controlsPanel.SetActive(!showAudio);

        _audioButton.interactable = !showAudio;
        _controlsButton.interactable = showAudio;
    }

    // Muestra los ajustes de audio
    public void ShowAudio()
    {
        ShowTab(true);
    }

    // TODO: añadir los controles del gamepad?
    // Muestra los controles (teclado + ratón)
    public void ShowControls()
    {
        ShowTab(false);
    }
}
