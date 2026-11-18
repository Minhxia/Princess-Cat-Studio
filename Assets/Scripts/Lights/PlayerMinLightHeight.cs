using UnityEngine;

// Clase mueve la luz con respecto a la altura del jugador
public class PlayerMinLightHeight : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float _heightRatio = 0.4f;

    private CharacterController _characterController;

    private void Awake()
    {
        _characterController = GetComponentInParent<CharacterController>();

        if (_characterController == null || transform.parent != _characterController.transform)
        {
            Debug.LogError("[PlayerMinLightHeight] La luz debe ser hija de Player con CharacterController.", this);
            enabled = false;
        }
    }

    private void LateUpdate()
    {
        float feetY = _characterController.center.y - _characterController.height / 2f;
        float lightY = feetY + _characterController.height * _heightRatio;

        Vector3 localPosition = transform.localPosition;
        localPosition.y = lightY;
        transform.localPosition = localPosition;
    }
}
