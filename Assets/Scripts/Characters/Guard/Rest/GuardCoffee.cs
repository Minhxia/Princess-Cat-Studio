using UnityEngine;
using UnityEngine.Events;

// Clase que gestiona el café del guardia (punto de la cafetera, su preparación y la taza).
public class GuardCoffee : MonoBehaviour
{
    #region Variables
    [Header("Coffee")]
    [SerializeField] private Transform _coffeePoint;    // punto delante de la cafetera
    [SerializeField] private GameObject _cup;           // taza para debug
    [SerializeField, Min(0f)] private float _preparationSeconds = 3f;   // tiempo de preparación
    [SerializeField, Range(0f, 1f)] private float _cupRevealProgress = 0.75f;  // momento en que se ve la taza

    [Header("Coffee machine feedback")]
    [SerializeField] private UnityEvent _onPreparationStarted = new UnityEvent();   // para asociarle un efecto de sonido
    [SerializeField] private UnityEvent _onCupTaken = new UnityEvent(); // para asociarle un efecto de sonido

    [Header("Animation")]
    [SerializeField] private Animator _animator;
    [SerializeField] private string _preparingParameter = "PrepararCafe";
    [SerializeField] private string _progressParameter = "CafeEnProceso";

    private bool _preparing;    // indica si está preparando el café
    private bool _cupTaken;     // indica si ya muestra la taza en la mano
    #endregion

    #region Getters
    public Transform CoffeePoint => _coffeePoint;

    public float PreparationSeconds => _preparationSeconds;
    #endregion

    private void Awake()
    {
        // se oculta la taza al inicio
        PutAwayCup();
    }

    // Gira el guardia hacia la cafetera, para que quede mirando hacia ella
    public bool FaceMachine()
    {
        // orientación de la cafetera
        Quaternion target = Quaternion.Euler(0f, _coffeePoint.eulerAngles.y, 0f);
        // el guardia gira progresivamente hacia ella
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, 180f * Time.deltaTime);
        // y hasta que no haya girado del todo devuelve false
        return Quaternion.Angle(transform.rotation, target) <= 0.1f;
    }

    #region Preparation
    // Comienza la preparación del café
    public void BeginPreparation()
    {
        // se oculta la taza de la vez anterior
        PutAwayCup();
        
        _preparing = true;
        SetPreparingAnimation(true);
        // se establece el progreso de preparación al 0%
        SetPreparationProgress(0f);
        _onPreparationStarted.Invoke();
    }

    // Actualiza la preparación del café (su progreso)
    public void SetPreparationProgress(float progress)
    {
        // si no está en proceso de preparación, sale
        if (!_preparing)
            return;

        // al recibir el valor del progreso, se convierte a un valor entre 0 y 1
        progress = Mathf.Clamp01(progress);
        // se muestra la animación de preparar el café
        if (_animator != null && !string.IsNullOrEmpty(_progressParameter))
            _animator.SetFloat(_progressParameter, progress);

        // y en función del progreso, se muestra la taza en la mano del guardia
        if (!_cupTaken && progress >= _cupRevealProgress)
        {
            _cupTaken = true;
            // si hay una taza asignada (para debug), se muestra
            if (_cup != null)
                _cup.SetActive(true);
            _onCupTaken.Invoke();
        }
    }

    // Termina de preparar el café
    public void FinishPreparation()
    {
        SetPreparationProgress(1f); // 1f = progreso al 100%
        _preparing = false;
        SetPreparingAnimation(false);
    }
    #endregion

    // Oculta la taza
    public void PutAwayCup()
    {
        _preparing = false;
        _cupTaken = false;

        // se comprueba que la taza es hija del guardia
        if (_cup != null && _cup.transform.IsChildOf(transform))
            _cup.SetActive(false);

        // se desactiva la animación de preparación
        SetPreparingAnimation(false);
    }

    #region Auxiliar
    // Cambia el bool que controla la animación
    private void SetPreparingAnimation(bool preparing)
    {
        if (_animator != null && !string.IsNullOrEmpty(_preparingParameter))
            _animator.SetBool(_preparingParameter, preparing);
    }
    #endregion
}
