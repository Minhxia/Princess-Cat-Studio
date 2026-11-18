using UnityEngine;

[DisallowMultipleComponent]

[RequireComponent(typeof(InteractableItem))]

// Clase que representa la placa de la estatua, que contiene la pista para resolver el puzzle
public class StatueCluePlaque : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private StatueTradePuzzle _puzzle;
    [SerializeField] private CluePanelUI _cluePanel;

    // Muestra el texto de la pista al interactuar con la placa
    public void Examine()
    {
        if (!isActiveAndEnabled || Time.timeScale <= 0f) return;

        if (_puzzle == null || _cluePanel == null)
        {
            Debug.LogWarning("[StatueCluePlaque] Asigna el puzzle y el panel de la pista.", this);
            return;
        }

        if (!_puzzle.IsReady) return;

        if (_cluePanel.ShowClue(_puzzle.Clue))
        {
            // se realiza el cambio del destello de la placa a la lámpara solo la primera vez
            _puzzle.NotifyClueExamined();
        }
    }
}
