using UnityEngine;

// se impide añadir dos componentes LightController al mismo GameObject
[DisallowMultipleComponent]

//[RequireComponent(typeof(LightDuration))]

// Clase que gestiona el encendido y apagado de las luces (mechero, lámpara, linterna)
public class LightController : MonoBehaviour, IHandUsable
{
    [Header("Light")]
    [SerializeField] private Light _lightSource;
    [SerializeField] private bool _startsLit; // indica si la luz empieza encendida

    private LightDuration _duration;

    public bool IsLit { get; private set; } // estado actual del objeto

    private void Awake()
    {   
        _duration = GetComponent<LightDuration>();
        
        // se comprueba que haya una luz asignada, que pertenezca al propio obnjeto
        // o a uno de sus hijos
        if (_lightSource == null || 
            _lightSource.transform != transform && !_lightSource.transform.IsChildOf(transform))
        {
            Debug.LogError("[LightController] Asigna la luz de este objeto o de uno de sus hijos.", this);
            enabled = false;
            return;
        }
    }

    private void Start()
    {
        // hay que dar tiempo al Awake de LightDuration, para inicializar la duración
        SetLit(_startsLit);
    }

    // Aplica el nuevo estado a la luz
    public void SetLit (bool lit)
    {
        if (_lightSource == null) return;

        // se comprueba la duración si la luz está encendida
        if (lit && _duration != null && !_duration.HasDurationRemaining)
        {
            lit = false;            
            Debug.Log("[LightController] Se acabó la duración.");
        }

        IsLit = lit;
        _lightSource.enabled = lit;

        Debug.Log("[LightController] La luz está encendida? " + lit);
    }

    // Alterna el estado de la luz (encendido <-> apagado), se llama desde PlayerHands
    public void Toggle()
    {
        SetLit(!IsLit);
    }

    // Al hacer click, se enciende/apaga la luz
    public void OnUsePressed()
    {
        Toggle();
    }

    // En este caso, soltar el click no apaga la luz, hace falta pulsar de nuevo
    public void OnUseReleased() { }
}
