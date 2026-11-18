using System;
using BehaviourAPI.StateMachines;

// Clase que guarda el estado y el método que se ejecuta al entrar en dicho estado.
// De esta manera, si el guardia ve al jugador, puede suspender su acción, mandar
// el aviso al monstruo y luego retomar esa acción (junto con su estado) dónde lo 
// había dejado (en lugar de empezar de nuevo).
public class MovementStateEntry
{
    private readonly Action<State, Action> _enterState;
    private readonly Action _startMethod;

    public State State { get; set; }

    public MovementStateEntry(Action<State, Action> enterState, Action startMethod)
    {
        _enterState = enterState;
        _startMethod = startMethod;
    }

    public void Start()
    {
        _enterState(State, _startMethod);
    }
}
