using UnityEngine;
using BehaviourAPI.Core;

// Acción del estado Descansar. Se encarga de gestionar el descanso del guardia (sentado
// en una silla).
public class RestAction
{
    private readonly MuseumGuardPatrolRoute _route;
    private readonly MuseumGuardNavigation _navigation;
    private readonly GuardRestAccess _access;
    private readonly float _restSeconds; // tiempo que está descansando
    private readonly bool _restOnStart; // indica si al comenzar la partida, el guardia empieza descansando
    private readonly System.Action _onFinished; // evento para avisar de que termina el descanso

    private bool _firstRest = true; // primer descanso de la partida
    private float _remaining;       // tiempo restante del descanso

    public RestAction(MuseumGuardPatrolRoute route, MuseumGuardNavigation navigation, 
        float restSeconds, bool restOnStart, System.Action onFinished, GuardRestAccess access = null)
    {
        _route = route;
        _navigation = navigation;
        _restSeconds = restSeconds;
        _restOnStart = restOnStart;
        _onFinished = onFinished;
        _access = access;
    }

    // Detiene al guardia y comienza su descanso
    public void Begin()
    {
        _navigation.Stop();
        _route.Reset();

        // si al comenzar la partida, no se indica que tenga que descansar al principio,
        // directamente sale de la sala inicial (descanso = 0s)
        _remaining = _firstRest && !_restOnStart ? 0f : _restSeconds;
        _firstRest = false;

        // se establece si está preparado para sentarse
        if (_access != null)
            _access.SetSitting(_access.ReadyToSit && _remaining > 0f);
    }

    // Actualiza el tiempo restante de descanso
    public Status Tick()
    {
        // se prepara la ruta para hacer la patrulla de nuevo
        if (!_route.IsReady)
            return Status.Running;
        
        // se actualiza el tiempo restante
        _remaining -= Time.deltaTime;
        // si no queda, se manda el aviso para terminar
        if (_remaining <= 0f)
            _onFinished();  // se ejecuta RestFinished del runner
        
        // si aún queda tiempo, continua descansando
        return Status.Running;
    }
}
