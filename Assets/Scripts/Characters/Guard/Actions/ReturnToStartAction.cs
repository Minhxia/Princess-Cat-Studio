using BehaviourAPI.Core;

// Acción del estado RetrocederSalaInicial. Se encargar de llevar al guardia al
// punto de descanso.
public class ReturnToStartAction
{
    private readonly MuseumGuardPatrolRoute _route;
    private readonly MuseumGuardNavigation _navigation;
    private readonly System.Action _onArrived;

    public ReturnToStartAction(MuseumGuardPatrolRoute route, MuseumGuardNavigation navigation,
                                    System.Action onArrived)
    {
        _route = route;
        _navigation = navigation;
        _onArrived = onArrived;
    }

    // Inicia el desplazamiento del guardia
    public void Begin()
    {
        _navigation.BeginTravel();
    }

    public Status Tick()
    {
        // primero se comprueba si ya no quedan puntos a los que ir
        if (!_route.HasPendingStep)
            return Status.Success;
        
        // si todavía queda alguno, se comprueba si ha llegado
        if (_navigation.CheckArrival() != Status.Success)
            return Status.Running;
        
        // si ya ha llegado
        _route.CompleteStep();
        // se comprueba si queda algún punto más
        if (_route.HasPendingStep)
        {
            // y si es así, comienza a desplazarse de nuevo
            _navigation.BeginTravel();
            return Status.Running;
        }

        // cuando llega al último punto, se avisa que va a comenzar a descansar
        _navigation.Stop();
        _onArrived(); // se ejecuta RestPointReached del runner
        return Status.Success;
    }
}
