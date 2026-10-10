using UnityEngine;

// se impide añadir dos componentes LightController al mismo GameObject
[DisallowMultipleComponent]

// Clase que gestiona el encendido y apagado de las luces (mechero, lámpara, linterna)
public class LightController : MonoBehaviour, IHandUsable
{
    [Header("Light")]
    [SerializeField] private Light _lightSource;
    [SerializeField] private bool _startsLit; // encendida

    public bool IsLit { get; private set; } // estado actual del objeto

    private void Awake()
    {   
        // se comprueba que haya una luz asignada, que pertenezca al propio obnjeto
        // o a uno de sus hijos
        if (_lightSource == null || 
            _lightSource.transform != transform && !_lightSource.transform.IsChildOf(transform))
        {
            Debug.LogError("[LightController] Asigna la luz de este objeto o de uno de sus hijos.", this);
            enabled = false;
            return;
        }

        SetLit(_startsLit);
    }
    
    // Aplica el nuevo estado a la luz
    public void SetLit (bool lit)
    {
        if (_lightSource == null) return;

        IsLit = lit;
        _lightSource.enabled = lit;

        Debug.Log("[LightController] La luz está encendida? " + lit);
    }

    // Alterna el estado de la luz (encendido <-> apagado)
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
