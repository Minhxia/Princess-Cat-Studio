using BehaviourAPI.Core;

// Acción del estado AvanzarSala. Se encarga de llevar al guardia por las puertas entre
// las salas (es decir, los puntos de cruce).
public class AdvanceRoomAction
{
    private readonly MuseumGuardPatrolRoute _route;
    private readonly MuseumGuardNavigation _navigation;

    // Auxiliar. Comprueba si toca una entrada (un punto de cruce).
    private bool HasDoorToCross => !_route.AtPatrolPoint && !_route.AtFinalReturn;

    public AdvanceRoomAction(MuseumGuardPatrolRoute route, MuseumGuardNavigation navigation)
    {
        _route = route;
        _navigation = navigation;
    }

    // Comienza a avanzar hacia la entrada
    public void Begin()
    {
        if (HasDoorToCross)
            _navigation.BeginTravel();
    }

    // Avanza por la ruta y comprueba si ha llegado
    public Status Tick()
    {
        // si no queda ninguna entrada que cruzar, termina
        if (!HasDoorToCross)
            return Status.Success;

        // se comprueba si el guardia ha llegado a una entrada
        if (_navigation.CheckArrival() == Status.Success)
        {
            // si ha llegado, se completa el paso y se comprueba de nuevo si hay más
            _route.CompleteStep();
            if (HasDoorToCross)
                _navigation.BeginTravel();
        }

        // si no, continua avanzando
        return Status.Running;
    }
}
