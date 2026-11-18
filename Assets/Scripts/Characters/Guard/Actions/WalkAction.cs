using BehaviourAPI.Core;

// Acción del estado Andar. Se encarga de llevar al guardia de un waypoint al siguiente.
public class WalkAction
{
    private readonly MuseumGuardPatrolRoute _route;
    private readonly MuseumGuardNavigation _navigation;

    public WalkAction(MuseumGuardPatrolRoute route, MuseumGuardNavigation navigation)
    {
        // se guardan la ruta y la navegación
        _route = route;
        _navigation = navigation;
    }

    // Inicia el desplazamiento del guardia
    public void Begin()
    {
        if (_route.AtPatrolPoint)
        {
            _navigation.BeginTravel();
        }
    }

    // Comprueba qué hacer
    public Status Tick()
    {
        // se comprueba el waypoint de destino y si es una puerta o se está volviendo
        // a la sala de descanso, termina
        if (!_route.AtPatrolPoint)
            return Status.Success;
        
        // se comprueba si ha llegado al waypoint y si ese punto tiene una parada/inspección
        Status arrival = _navigation.CheckArrival();
        if (arrival != Status.Success || _route.CurrentWaitSeconds > 0f) 
            return arrival;

        // si ya ha llegado y es un punto de paso, avanza al siguiente
        _route.CompleteStep();
        // se vuelve a comprobar si el siguiente waypoint es una puerta o la sala de descanso,
        // si es así termina
        if (!_route.AtPatrolPoint)
            return Status.Success;
        // si no, si ese siguiente waypoint es otro punto, el guardia continua andando
        _navigation.BeginTravel();
        return Status.Running;
    }
    
}
