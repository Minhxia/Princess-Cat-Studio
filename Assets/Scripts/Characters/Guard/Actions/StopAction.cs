using BehaviourAPI.Core;

// Acción del estado Parar. Se encarga de parar al guardia cuando llega
// a un punto de inspección.
public class StopAction
{
    private readonly MuseumGuardPatrolRoute _route;
    private readonly MuseumGuardNavigation _navigation;
    private readonly MuseumGuardInspection _inspection;

    public StopAction(MuseumGuardPatrolRoute route, MuseumGuardNavigation navigation,
                        MuseumGuardInspection inspection)
    {
        _route = route;
        _navigation = navigation;
        _inspection = inspection;        
    }

    // Detiene al guardia y prepara la inspección
    public void Begin()
    {
        _navigation.Stop();
        _inspection.Prepare(_route.CurrentTarget.forward);
    }

    // Comienza la inspección, es decir, el guardia comienza a girar para mirar
    // a un lado y al otro
    public Status Tick()
    {
        // gira un poco cada vez
        return _inspection.FaceStart() ? Status.Success : Status.Running;
    }
}
