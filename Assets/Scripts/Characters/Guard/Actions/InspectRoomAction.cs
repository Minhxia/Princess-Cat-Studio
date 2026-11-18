using UnityEngine;
using BehaviourAPI.Core;

// Acción del estado ComprobarSala. Se encarga de controlar la inspección que realiza
// el guardia en una sala.
public class InspectRoomAction
{
    private readonly MuseumGuardPatrolRoute _route;
    private readonly MuseumGuardInspection _inspection;
    private float _elapsed; // tiempo transcurrido desde que empieza la inspección

    public InspectRoomAction(MuseumGuardPatrolRoute route, MuseumGuardInspection inspection)
    {
        _route = route;
        _inspection = inspection;
    }

    // Comienza la inspección
    public void Begin()
    {
        _elapsed = 0f;
    }

    public Status Tick()
    {
        // se acumula el tiempo
        _elapsed += Time.deltaTime;
        // se calcula el progreso para ir aplicando el giro de la inspección
        // en función de ese tiempo
        float progress = Mathf.Clamp01(_elapsed / _route.CurrentWaitSeconds);
        _inspection.SetProgress(progress);

        // se comprueba si ha terminado la inspección
        return progress >= 1f ? Status.Success : Status.Running;
    }
}
