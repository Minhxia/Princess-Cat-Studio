using System.Collections.Generic;
using UnityEngine;

// fases de la partida
public enum GamePhase
{
    Playing,    // jugando
    Victory,    // victoria, el jugador ha conseguido salvar a todos los niños
    Defeat      // derrota, uno de los monstruos ha detectado al jugador o los niños, y los ha matado
}

[DisallowMultipleComponent]

// Clase que gestiona el estado y el progreso de la partida.
public class GameManager : MonoBehaviour
{
    [Header("Game objetive")]
    [SerializeField, Min(1)] private int _requiredChildren = 3; // número de niños que tiene que rescatar
    [SerializeField] private PlayerHands _playerHands;  // para comprobar si lleva a un niño de la mano
    [SerializeField] private GameEndController _gameEndController;  // para el fin de partida

    // lista con los niños rescatados
    private readonly List<ChildCompanion> _rescuedChildren = new List<ChildCompanion>();

    #region Getters
    public static GameManager Instance { get; private set; }
    public GamePhase CurrentPhase { get; private set; }
    public bool IsPlaying => CurrentPhase == GamePhase.Playing;
    public int RescuedChildrenCount => _rescuedChildren.Count;  // recuento de niños rescatados
    public int RequiredChildren => _requiredChildren;
    #endregion

    #region Unity
    private void Awake()
    {
        // primero se comprueba que no haya más de un GameManager en la escena!
        if (Instance != null && Instance != this)
        {
            Debug.LogError("[GameManager] Hay más de un GameManager en la partida.", this);
            enabled = false;
            return;
        }

        // se guarda la instancia
        Instance = this;
        // y se establece el estado inicial
        CurrentPhase = GamePhase.Playing;

        // si no se asigna desde el Inspector, se busca en el propio objeto
        if (_gameEndController == null)
            _gameEndController = GetComponent<GameEndController>();

        if (_playerHands == null)
            Debug.LogError("[GameManager] Asigna PlayerHands en el Inspector.", this);

        if (_gameEndController == null)
            Debug.LogError("[GameManager] Asigna GameEndController en el Inspector.", this);
    }

    private void OnDestroy()
    {
        // al destruirse, se resetea la referencia a la instancia
        if (Instance == this)
            Instance = null;
    }
    #endregion

    // Registra el rescate de un niño y termina el juego con victoria si
    // ha rescatado a todos
    public bool TryRescueChild(ChildCompanion child)
    {
        if (!isActiveAndEnabled || !IsPlaying || child == null || _playerHands == null
                || _gameEndController == null || !_gameEndController.isActiveAndEnabled)
            return false;

        // en caso de rescatar al mismo niño más de una vez, o no estar acompañando a ninguno, sale
        if (_rescuedChildren.Contains(child) || !_playerHands.IsAccompanying(child))
            return false;

        // si no se puede cargar la pantalla de GameOver, no se cuenta al último niño
        if (RescuedChildrenCount + 1 >= _requiredChildren && !_gameEndController.CanEndGame())
            return false;

        // se suelta la mano del niño
        child.ReleaseHand();
        // si no se puede, sale
        if (_playerHands.IsAccompanying(child))
        {
            Debug.LogError("[GameManager] No se puede soltar la mano del niño: " + child.name);
            return false;
        }

        // se añade el niño al recuento
        _rescuedChildren.Add(child);
        child.gameObject.SetActive(false);
        Debug.Log("[GameManager] Niños a salvo: " + RescuedChildrenCount + "/" + _requiredChildren);

        // si ya se ha rescatado a todos
        if (RescuedChildrenCount >= _requiredChildren)
        {
            // se actualiza a la fase de victoria y se muestra el fin de partida
            CurrentPhase = GamePhase.Victory;
            _gameEndController.Win();
        }

        return true;
    }

    // Termina el juego con derrota
    public void Lose()
    {
        if (!isActiveAndEnabled || !IsPlaying || _gameEndController == null
            || !_gameEndController.isActiveAndEnabled)
            return;

        // se comprueba si se puede terminar la partida
        if (!_gameEndController.CanEndGame())
            return;
        // y si es así, actualiza a la fase de derrota
        CurrentPhase = GamePhase.Defeat;
        _gameEndController.Lose();
    }
}
