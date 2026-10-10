using UnityEngine;

[DisallowMultipleComponent]

// Clase que gestiona la estamina del jugador
public class PlayerStamina : MonoBehaviour
{
    #region Variables
    [Header("Stamina")]
    [SerializeField, Min(0.1f)] private float _maxStamina = 5f;
    [SerializeField, Min(0f)] private float _drainPerSecond = 1f;
    [SerializeField, Min(0.01f)] private float _recoveryPerSecond = 1.25f;
    [SerializeField, Min(0f)] private float _recoveryDelay = 1.5f; // tiempo que tarda en empezar a regenerarse la estamina después de dejar de correr

    private float _currentStamina;
    private float _recoveryTimer; // tiempo que falta para que empiece a regenerarse la estamina
    private bool _exhausted; // impide volver a correr aunque la barra ya se haya regenerado un poco ("estás exhausto")

    public bool IsRunning { get; private set; }
    // valor entre 0 y 1 que representa el porcentaje de estamina actual
    public float FillAmount => _currentStamina / _maxStamina;
    // si el jugador está corriendo o si la estamina no se ha regenerado, se muestra la barra
    public bool ShouldShowBar => IsRunning || _currentStamina < _maxStamina;
    #endregion

    #region Unity Methods
    private void Awake()
    {
        // se establecen los valores mínimos para evitar errores
        // (como dividir por cero o tener valores negativos)
        _maxStamina = Mathf.Max(0.1f, _maxStamina);
        _drainPerSecond = Mathf.Max(0f, _drainPerSecond);
        _recoveryPerSecond = Mathf.Max(0.01f, _recoveryPerSecond);
        _recoveryDelay = Mathf.Max(0f, _recoveryDelay);
        _currentStamina = _maxStamina;
    }
    #endregion

    #region Public Methods
    // Actualiza la estamina del jugador e indica si está corriendo
    public bool UpdateStamina(bool wantsToRun, bool canMove)
    {
        bool wasRunning = IsRunning; // se guarda el estado anterior
        // se determina si el jugador puede correr AHORA
        IsRunning = wantsToRun && canMove && !_exhausted && _currentStamina > 0f;

        // si el jugador está corriendo AHORA, se gasta la estamina
        if (IsRunning)
        {
            _currentStamina = Mathf.Max(
                0f,
                _currentStamina - _drainPerSecond * Time.deltaTime
            );
            
            // si justo se vacía la estamina, deja de correr
            if (_currentStamina == 0f)
            {
                IsRunning = false;
                _exhausted = true;
                _recoveryTimer = _recoveryDelay;
            }
            //Debug.Log("[PlayerStamina] Jugador está corriendo AHORA. Estamina actual: " + _currentStamina.ToString("F2") + " / " + _maxStamina.ToString("F2"));
        }
        // si el jugador deja de correr de manera voluntaria (suelta Shift) antes de
        // vaciar la estamina, se inicia su recuperación
        else if (wasRunning)
        {
            _recoveryTimer = _recoveryDelay;
            //Debug.Log("[PlayerStamina] Jugador ha dejado de correr voluntariamente. Estamina actual: " + _currentStamina.ToString("F2") + " / " + _maxStamina.ToString("F2"));
        }

        // si el jugador no está corriendo, se recupera la estamina
        if (!IsRunning)
        {
            if (_recoveryTimer > 0f)
            {
                _recoveryTimer = Mathf.Max(0f, _recoveryTimer - Time.deltaTime);
            }
            else
            {
                _currentStamina = Mathf.Min(
                    _maxStamina,
                    _currentStamina + _recoveryPerSecond * Time.deltaTime
                );

                // si el jugador está EXHAUSTO, no podrá volver a correr hasta que
                // la estmina se recupere por completo
                if (_currentStamina == _maxStamina)
                {
                    _exhausted = false;
                }
            }
            //Debug.Log("[PlayerStamina] Jugador no está corriendo. Estamina actual: " + _currentStamina.ToString("F2") + " / " + _maxStamina.ToString("F2") + ", tiempo de recuperación restante: " + _recoveryTimer.ToString("F2"));
        }

        return IsRunning;
    }

    // Para al jugador
    public void StopRunning()
    {
        if (IsRunning)
        {            
            _recoveryTimer = _recoveryDelay;
            IsRunning = false;
        }
    }
    #endregion
}
