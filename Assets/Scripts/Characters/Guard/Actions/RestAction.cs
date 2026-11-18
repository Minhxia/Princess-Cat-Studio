using UnityEngine;
using BehaviourAPI.Core;

// Acción del estado Pausa. Se encarga de parar al guardia (está en su descanso) y 
// preparar la ruta para la siguiente patrulla.
public class RestAction
{
    private readonly MuseumGuardPatrolRoute _route;
    private readonly MuseumGuardNavigation _navigation;
    private readonly float _restSeconds;
    private readonly bool _restOnStart; // indica si al comenzar la partida, el guardia empieza descansando
    private readonly System.Action _onFinished; // evento para avisar de que termina el descanso

    private bool _firstRest = true; // primer descanso de la partida
    private float _remaining;       // tiempo restante del descanso

    public RestAction(MuseumGuardPatrolRoute route, MuseumGuardNavigation navigation,
                        float restSeconds, bool restOnStart, System.Action onFinished)
    {
        _route = route;
        _navigation = navigation;
        _restSeconds = restSeconds;
        _restOnStart = restOnStart;
        _onFinished = onFinished;
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
