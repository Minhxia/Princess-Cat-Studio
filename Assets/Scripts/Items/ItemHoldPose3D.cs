using UnityEngine;

// Clase que indica la posición y orientación del objeto cuando se sujeta en una  mano
public class ItemHoldPose3D : MonoBehaviour
{
    [SerializeField] private Vector3 _localPosition;
    [SerializeField] private Vector3 _localEulerAngles;

    public Vector3 LocalPosition => _localPosition;
    public Quaternion LocalRotation => Quaternion.Euler(_localEulerAngles);
}
