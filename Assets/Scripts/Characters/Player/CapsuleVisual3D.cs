using UnityEngine;

// Clase auxiliar que gestiona la visualización correcta de la cápsula al agacharse
// (reduce su tamaño a la mitad, se ve aplastada)
// TODO: se borrará cuando se pongan los sprites de los personajes
public class CapsuleVisual3D : MonoBehaviour
{
    private CharacterController _controller;

    private Vector3 _standingLocalPosition;
    private Vector3 _standingLocalScale;
    private float _standingHeight;

    private void Awake()
    {
        _controller = GetComponentInParent<CharacterController>();

        _standingLocalPosition = transform.localPosition;
        _standingLocalScale = transform.localScale;
        _standingHeight = _controller.height;
    }

    private void LateUpdate()
    {
        float currentHeight = _controller.height;
        float heightRatio = currentHeight / _standingHeight;

        // se reduce la altura visible, conservando la anchura
        transform.localScale = new Vector3(
            _standingLocalScale.x,
            _standingLocalScale.y * heightRatio,
            _standingLocalScale.z
        );

        // se baja el centro visual para mantener los pies en el suelo
        transform.localPosition = _standingLocalPosition
            - Vector3.up * (_standingHeight - currentHeight) / 2f;
    }
}