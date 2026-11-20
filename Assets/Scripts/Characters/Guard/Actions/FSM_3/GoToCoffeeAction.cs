using BehaviourAPI.Core;

// Acción del estado IrCafetera. Se encargar de llevar al guardia al punto de la cafetera/máquina.
public class GoToCoffeeAction
{
    private readonly GuardCoffee _coffee;
    private readonly MuseumGuardNavigation _navigation;
    private bool _arrived;  // indica si el guardia ha llegado al punto donde está la cafetera

    public GoToCoffeeAction(GuardCoffee coffee, MuseumGuardNavigation navigation)
    {
        _coffee = coffee;
        _navigation = navigation;
    }

    // Inicia el desplazamiento hacia la cafetera
    public void Begin()
    {
        _arrived = false;

        if (_coffee != null)
            _navigation.BeginTravel(_coffee.CoffeePoint, false);
    }

    public Status Tick()
    {
        if (_coffee == null)
            return Status.Success;

        // se comprueba si ha llegado hasta la cafetera
        if (!_arrived)
        {
            if (_navigation.CheckArrival() != Status.Success)
                return Status.Running;

            // si es así, se detiene
            _navigation.Stop();
            _navigation.SetManualRotation();    // para poder girarlo hacia la cafetera
            _arrived = true;
        }


        // se gira al guardia hacia la cafetera
        if (_coffee.FaceMachine())
            return Status.Success;

        return Status.Running;
    }
}
