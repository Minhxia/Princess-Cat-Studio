using UnityEngine;
using TMPro;

// Clase que gestiona el panel de lectura de la pista
public class CluePanelUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject _panel;
    [SerializeField] private TMP_Text _clueText;

    // indica si el panel de la pista está activo y mostrándose
    public bool IsOpen => _panel != null && _panel.activeInHierarchy;

    private void Awake()
    {
        Close();
    }

    // Muestra el panel con el texto de la pista
    public bool ShowClue(string clue)
    {
        if (!isActiveAndEnabled || _panel == null || _clueText == null 
                || string.IsNullOrWhiteSpace(clue))
        {
            Debug.LogWarning("[CluePanelUI] Asigna todo lo necesario en el Inspector.", this);
            return false;
        }

        _clueText.text = clue;
        _panel.SetActive(true);
        return _panel.activeInHierarchy;
    }

    // Cierra el panel de la pista
    public void Close()
    {
        if (_panel != null)
            _panel.SetActive(false);
    }
}
