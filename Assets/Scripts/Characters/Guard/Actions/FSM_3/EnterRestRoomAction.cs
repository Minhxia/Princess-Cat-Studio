using UnityEngine;
using BehaviourAPI.Core;

// indica en qué fase está el guardia al entrar
public enum RestEntryStage
{
    Reader,     // se dirige al panel para utilizar la tarjeta
    Door,       // espera a que la puerta se abra
    Inside,     // cruza la puerta al interior de la garita
    Seat,       // se dirige a donde está la silla
    FacingSeat, // se coloca en la orientación correcta para sentarse
    Outside,    // se dirige a descansar en la tienda de regalos (no tiene la tarjeta)
    Done        // ha terminado de prepararse para descansar
}

// Acción del estado PrepararDescanso. Se encarga de preparar al guardia para descansar en
// la tienda de regalos o en la garita en función de si tiene la tarjeta.
public class EnterRestRoomAction
{
    #region Variables
    private readonly GuardRestAccess _access;   // referencia a la clase que gestiona el panel, la puerta, la silla, etc.
    private readonly MuseumGuardNavigation _navigation;
    private readonly Transform _outsidePoint;   // punto de descanso fuera (no tiene la tarjeta)
    private RestEntryStage _stage;  // indica en qué fase está
    private bool _admitted; // indica que tiene la tarjeta y se mantiene la puerta abierta
    #endregion

    public EnterRestRoomAction(GuardRestAccess access, MuseumGuardNavigation navigation,
                                   Transform outsidePoint)
    {
        _access = access;
        _navigation = navigation;
        _outsidePoint = outsidePoint;
    }

    // Decide dónde va a preparar el descanso
    public void Begin()
    {
        // se resetea la información
        _stage = RestEntryStage.Done;
        _admitted = false;
        if (_access != null)
            _access.SetReadyToSit(false);

        // si no tiene la tarjeta
        if (!_access.HasCard)
        {
            // se dirige al punto de descanso en la tienda de regalos
            if (_outsidePoint != null)
            {
                _navigation.BeginTravel(_outsidePoint, false);
                _stage = RestEntryStage.Outside;
            }
            return;
        }

        // si la tiene, se dirige al panel de seguridad de la puerta de la garita
        _stage = RestEntryStage.Reader;
        _navigation.BeginTravel(_access.ReaderPoint, false);
    }

    #region Phases
    public Status Tick()
    {
        // en caso de haber terminado de prepararse, sale
        if (_stage == RestEntryStage.Done)
            return Status.Success;

        // si el jugador le roba la tarjeta cuando está entrando a la garita,
        bool enteringRoom = _stage == RestEntryStage.Reader || _stage == RestEntryStage.Door
                                || _stage == RestEntryStage.Inside;
        // cambia su punto de descanso
        if (enteringRoom && !_access.HasCard && !_access.IsInsideRoom)
        {
            _navigation.BeginTravel(_outsidePoint, false);
            _stage = RestEntryStage.Outside;
        }

        // se comprueba en qué fase está el guardia
        switch (_stage)
        {
            // se dirige al panel para utilizar la tarjeta 
            case RestEntryStage.Reader:
                // se comprueba si ha llegado
                if (_navigation.CheckArrival() == Status.Success)
                {
                    // si es así, se para
                    _navigation.Stop();
                    // y utiliza la tarjeta en el panel para abrir la puerta
                    if (_access.ReadCard())
                    {
                        _admitted = true;
                        _access.Door.HoldOpenForGuard();
                        // se actualiza la fase
                        _stage = RestEntryStage.Door;
                    }
                    // si no puede, se dirige al punto de la tienda de regalos
                    else
                    {
                        _navigation.BeginTravel(_outsidePoint, false);
                        // se actualiza la fase
                        _stage = RestEntryStage.Outside;
                    }
                }
                break;

            // espera a que la puerta se abra
            case RestEntryStage.Door:
                // cuando la puerta está abierta del todo, se dirige dentro de la garita
                if (_access.Door.IsFullyOpen && !_access.IsUsingCard)
                {
                    _navigation.BeginTravel(_access.InsidePoint, false);
                    // se actualiza la fase
                    _stage = RestEntryStage.Inside;
                }
                break;

            // cruza la puerta al interior de la garita
            case RestEntryStage.Inside:
                // se comprueba si ya está dentro 
                if (_navigation.CheckArrival() == Status.Success)
                {
                    _access.SetInsideRoom(true);
                    // se cierra la puerta
                    _access.Door.ReleaseGuardPassage();
                    _admitted = false;
                    // se dirige a la silla
                    _navigation.BeginTravel(_access.SeatPoint, false);
                    // se actualiza la fase
                    _stage = RestEntryStage.Seat;
                }
                break;

            // se dirige a donde está la silla
            case RestEntryStage.Seat:
                // se comprueba si ya está al lado de la silla
                if (_navigation.CheckArrival() == Status.Success)
                {
                    // se para y se gira manualmente para orientarlo hacia la silla interior
                    _navigation.Stop();
                    _navigation.SetManualRotation();
                    // se actualiza la fase
                    _stage = RestEntryStage.FacingSeat;
                }
                break;

            // se coloca en la orientación correcta para sentarse
            case RestEntryStage.FacingSeat:
                Transform seatPoint = _outsidePoint;

                // si es la silla interior, sólo se actualiza el punto
                if (_access.IsInsideRoom)
                    seatPoint = _access.SeatPoint;

                // en cambio si es la silla exterior, se gira progresivamente al guardia
                // y después, se actualiza la fase
                if (_access.FaceSeat(seatPoint)) {
                    _access.SetReadyToSit(true);
                    _stage = RestEntryStage.Done;
                }

                break;

            // se dirige a descansar en la tienda de regalos (no tiene la tarjeta)
            case RestEntryStage.Outside:
                // se comprueba si ha llegado al punto
                if (_navigation.CheckArrival() == Status.Success)
                {
                    // se para
                    _navigation.Stop();

                    // se resetea la variable por si acaso
                    if (_admitted)
                        _access.Door.ReleaseGuardPassage();
                    _admitted = false;

                    // se gira manualmente para orientarlo hacia la silla exterior
                    _navigation.SetManualRotation();
                    // y se actualiza la fase
                    _stage = RestEntryStage.FacingSeat;
                }
                break;
        }

        // ha terminado de prepararse para descansar
        if (_stage == RestEntryStage.Done)
            return Status.Success;

        return Status.Running;
    }
    #endregion
}
