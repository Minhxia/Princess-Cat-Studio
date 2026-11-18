using UnityEngine;
using UnityEngine.UI;

// Clase que gestiona la barra de duración de una luz (visual, ui)
public class HeldLightDurationUI : MonoBehaviour
{
    [SerializeField] private PlayerHands _hands;

    [Header("Left hand")]
    [SerializeField] private GameObject _leftBarRoot;
    [SerializeField] private Image _leftFillImage;

    [Header("Right hand")]
    [SerializeField] private GameObject _rightBarRoot;
    [SerializeField] private Image _rightFillImage;


    private void LateUpdate()
    {
        if (_hands == null) return;

        UpdateBar(_hands.LeftItem, _leftBarRoot, _leftFillImage);
        UpdateBar(_hands.RightItem, _rightBarRoot, _rightFillImage);
    }

    // Actualiza la barra de duración de una luz y la muestra cuando corresponda
    private void UpdateBar(GameObject item, GameObject barRoot, Image fill)
    {
        if (barRoot == null || fill == null) return;

        LightDuration duration = item != null ? item.GetComponent<LightDuration>() : null;
        bool showBar = duration != null;

        if (showBar)
        {
            fill.fillAmount = duration.FillAmount;
        }

        if (barRoot.activeSelf != showBar)
        {
            barRoot.SetActive(showBar);
        }
    }
}
