using UnityEngine;
using UnityEngine.AI;

// Clase que se encarga de girar al guardia cuando inspecciona una sala.
public class MuseumGuardInspection
{
    private readonly NavMeshAgent _agent;
    private readonly Transform _guard;
    private readonly float _halfAngle;
    private Quaternion _center;

    public MuseumGuardInspection(NavMeshAgent agent, float inspectionAngle)
    {
        _agent = agent;
        _guard = agent.transform;
        _halfAngle = inspectionAngle * 0.5f; // por defecto, está puesto a 90º, así que empieza en 45º
    }

    // Calcula el centro para hacer la rotación (inspección)
    public void Prepare(Vector3 waypointForward)
    {
        _agent.updateRotation = false;
        Vector3 forward = Vector3.ProjectOnPlane(waypointForward, Vector3.up);
        // si la proyección queda muy pequeña, se usa como centro la orientación horizontal del guardia
        _center = forward.sqrMagnitude > 0.001f ? Quaternion.LookRotation(forward, Vector3.up)
                                                : Quaternion.Euler(0f, _guard.eulerAngles.y, 0f);
    }

    // Gira al guardia hacia la derecha, para comenzar la inspección
    public bool FaceStart()
    {
        // orientación del extremo dcho
        Quaternion start = RotationAt(_halfAngle);
        // velocidad angular
        float turnSpeed = Mathf.Max(1f, _agent.angularSpeed);
        // se gira al guardia hacia la dcha de manera progresiva
        _guard.rotation = Quaternion.RotateTowards(_guard.rotation, start, turnSpeed * Time.deltaTime);
        return Quaternion.Angle(_guard.rotation, start) <= 0.1f;
    }

    // Realiza la inspección (ahora girando hacia la izda)
    public void SetProgress(float progress)
    {
        _guard.rotation = RotationAt(Mathf.Lerp(_halfAngle, -_halfAngle, progress));
    }

    // Calcula la orientación relativa al centro
    private Quaternion RotationAt(float yaw)
    {
        return Quaternion.AngleAxis(yaw, Vector3.up) * _center;
    }
}
