using UnityEngine;
using BehaviourAPI.Core;

// indica en qué fase está el guardia al salir
public enum RestExitStage
{
    StandUp,    // espera a que finalice el tiempo de la animación de levantarse
    Inside,     // se dirige al punto interior junto a la puerta (cuando está en la garita)
    Door,       // espera a que la puerta se abra del todo
    Outside,    // avanza y cruza el punto exterior (el que está al otro lado de la puerta, tienda de regalos)
    Done        // ha completado la preparación para salir
}

// Acción del estado PrepararSalida. Se encarga de preparar al guardia para volver a
// realizar otra patrulla.
public class LeaveRestRoomAction
{
    private readonly GuardRestAccess _access;
    private readonly MuseumGuardNavigation _navigation;
    private readonly Transform _outsidePoint;   // punto de descanso de la tienda de regalos
    private RestExitStage _stage;   // se guarda en qué fase está
    private float _standRemaining;  // tiempo restante para terminar de levantarse (animación)

    public LeaveRestRoomAction(GuardRestAccess access, MuseumGuardNavigation navigation,
                                    Transform outsidePoint)
    {
        _access = access;
        _navigation = navigation;
        _outsidePoint = outsidePoint;
    }

    // Decide si tiene que levantarse
    public void Begin()
    {
        // se resetea la fase
        _stage = RestExitStage.Done;

        if (_access == null)
            return;

        // si el guardia está sentado, se prepara para levantarse
        _access.SetReadyToSit(false);
        if (_access.IsSitting)
        {
            _access.SetSitting(false);
            // se asigna el tiempo para levantarse
            _standRemaining = _access.StandUpSeconds;
            // y se actualiza la fase
            _stage = RestExitStage.StandUp;
        }
        // si no lo está, comienza a salir
        else
        {
            BeginExit();
        }
    }

    #region Phases
    public Status Tick()
    {
        switch (_stage)
        {
            // espera a que finalice el tiempo de la animación de levantarse
            case RestExitStage.StandUp:
                _standRemaining -= Time.deltaTime;
                // cuando termina, se dispone para salir
                if (_standRemaining <= 0f)
                    BeginExit();
                break;
            
            // se dirige al punto interior junto a la puerta (cuando está en la garita)
            case RestExitStage.Inside:
                // se comprueba si ya ha llegado a la puerta
                if (_navigation.CheckArrival() == Status.Success)
                {
                    // se para
                    _navigation.Stop();
                    // se mantiene la puerta abierta
                    _access.Door.HoldOpenForGuard();
                    // y se actualiza la fase
                    _stage = RestExitStage.Door;
                }
                break;

            // espera a que la puerta se abra del todo
            case RestExitStage.Door:
                // se comprueba si la puerta está abierta del todo
                if (_access.Door.IsFullyOpen)
                {
                    // comienza a dirigirse al punto exterior de la garita
                    _navigation.BeginTravel(_outsidePoint, false);
                    // y se actualiza la fase
                    _stage = RestExitStage.Outside;
                }
                break;
            
            // avanza y cruza el punto exterior (el que está al otro lado de la puerta, tienda de regalos)
            case RestExitStage.Outside:
                // se comprueba si ya ha llegado a ese punto 
                if (_navigation.CheckArrival() == Status.Success)
                {
                    // entonces, se para y se actualiza la info
                    _navigation.Stop();
                    _access.SetInsideRoom(false);
                    // ya no es necesario mantener la puerta abierta para el guardia
                    _access.Door.ReleaseGuardPassage();
                    // se actualiza la fase
                    _stage = RestExitStage.Done;
                }
                break;
        }

        // ha completado la preparación para salir
        if (_stage == RestExitStage.Done)
            return Status.Success;
        
        return Status.Running;
    }
    #endregion

    // Comprueba dónde se encuentra el guardia e inicia su desplazamiento si
    // se encuentra dentro de la garita
    private void BeginExit()
    {
        // se comprueba si ya está fuera porque estaba descansando en la silla exterior
        if (!_access.IsInsideRoom)
        {
            // si es así, sólo se actualiza la fase y sale
            _stage = RestExitStage.Done;
            return;
        }

        // si no, comienza a moverse hacia el punto interior para cruzar la puerta
        _navigation.BeginTravel(_access.InsidePoint, false);
        _stage = RestExitStage.Inside;
    }
}
