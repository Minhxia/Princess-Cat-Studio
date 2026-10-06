using UnityEngine;
using UnityEngine.UI;

public class StaminaBarUI : MonoBehaviour
{
    [SerializeField] private PlayerStamina _stamina;
    [SerializeField] private GameObject _barRoot;
    [SerializeField] private Image _fillImage;

    private void LateUpdate()
    {
        if (_stamina == null || _barRoot == null || _fillImage == null) return;

        // se actualiza la barra de estamina
        _fillImage.fillAmount = _stamina.FillAmount;
        
        // se muestra u oculta la barra de estamina según corresponda
        // (solo se muestra si el jugador está corriendo o si la barra no está llena)
        if (_barRoot.activeSelf != _stamina.ShouldShowBar)
        {
            _barRoot.SetActive(_stamina.ShouldShowBar);
        }
    }
    
}
