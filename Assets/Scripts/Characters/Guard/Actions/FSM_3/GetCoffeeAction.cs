using UnityEngine;
using BehaviourAPI.Core;

// Acción del estado PrepararCafe. Se encarga de controlar el tiempo de preparación del café.
public class GetCoffeeAction
{
    private readonly GuardCoffee _coffee;
    private float _elapsed;     // tiempo transcurrido
    private float _duration;    // tiempo que tarda en preparar el café

    public GetCoffeeAction(GuardCoffee coffee)
    {
        _coffee = coffee;
    }

    // Comienza la preparación del café
    public void Begin()
    {
        _elapsed = 0f;

        if (_coffee == null)
            return;

        _duration = _coffee.PreparationSeconds;
        _coffee.BeginPreparation();
    }

    // Actualiza el tiempo
    public Status Tick()
    {
        if (_coffee == null)
            return Status.Success;

        // se acumula el tiempo
        _elapsed += Time.deltaTime;
        // y se comprueba el progreso de la preparación (cuando ha terminado = 1f)
        float progress = 1f;
        if (_duration > 0f)
            progress = Mathf.Clamp01(_elapsed / _duration);

        _coffee.SetPreparationProgress(progress);

        // si ha terminado, sale
        if (progress >= 1f)
        {
            _coffee.FinishPreparation();
            return Status.Success;
        }

        return Status.Running;
    }
}
