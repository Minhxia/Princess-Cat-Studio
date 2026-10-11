using UnityEngine;

[DisallowMultipleComponent]

[RequireComponent(typeof(LightController))]
// Clase que gestiona la duración limitada de una fuente de luz
public class LightDuration : MonoBehaviour
{
    public enum RefillType
    {
        None,
        Battery,
        Oil
    }

    [SerializeField, Min(0.1f)] private float _maxDurationSeconds = 180f;
    [SerializeField] private RefillType _refillType = RefillType.None;

    private LightController _lightController;
    private float _remainingSeconds;
    private float _initialDurationSeconds;

    public float RemainingSeconds => _remainingSeconds;
    public bool HasDurationRemaining => _remainingSeconds > 0f;
    public float FillAmount => _remainingSeconds / _initialDurationSeconds;

    private void Awake()
    {
        _lightController = GetComponent<LightController>();
        _initialDurationSeconds = Mathf.Max(0.1f, _maxDurationSeconds);
        _remainingSeconds = _initialDurationSeconds;
    }

    private void Update()
    {
        // si no está encendida o no le queda duración, sale
        if (!_lightController.IsLit || !HasDurationRemaining) return;

        // se calculan los segundos restantes
        _remainingSeconds = Mathf.Max(0f, _remainingSeconds - Time.deltaTime);
        Debug.Log("[LightDuration] Segundos restantes: " + _remainingSeconds);

        // en caso de que se agoten, se apaga
        if (!HasDurationRemaining)
        {
            _lightController.SetLit(false);
        }
    }

    // Indica si se ha recuperado algo de duración
    public bool TryRestore(RefillType type, float seconds)
    {
        if (type == RefillType.None || type != _refillType ||
                seconds < 0f || _remainingSeconds >= _initialDurationSeconds)
        {
            return false;
        }

        // se suman los segundos, siempre y cuando no superen el máximo de duración
        _remainingSeconds = Mathf.Min (
            _initialDurationSeconds,
            _remainingSeconds + seconds
        );
        Debug.Log("[LightDuration] Recarga realizada. Remaining seconds: " + _remainingSeconds);

        return true;
    }
}
