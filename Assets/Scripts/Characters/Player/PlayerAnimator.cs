using UnityEngine;

[DisallowMultipleComponent]

[RequireComponent(typeof(CharacterController), typeof(CharacterMovement3D))]

//Clase que controla las animaciones de los brazos/manos.
public class PlayerArmsAnimator : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator _animator;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float _movementThreshold = 0.05f;
    [SerializeField] private string _movingParameter = "Caminar";
    [SerializeField] private string _runningParameter = "Correr";

    [Header("Pickup")]
    [SerializeField] private string _pickLeftParameter = "CogerIzquierda";
    [SerializeField] private string _pickRightParameter = "CogerDerecha";
    [SerializeField] private string _pickBothParameter = "CogerAmbas";

    [Header("Drop")]
    [SerializeField] private string _dropLeftParameter = "SoltarIzquierda";
    [SerializeField] private string _dropRightParameter = "SoltarDerecha";

    private CharacterController _characterController;
    private CharacterMovement3D _movement;

    #region Unity
    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _movement = GetComponent<CharacterMovement3D>();

        if (_animator != null)
            _animator.applyRootMotion = false;
    }

    private void LateUpdate()
    {
        if (_animator == null || _characterController == null || _movement == null)
            return;

        // si el juego se pausa, se para la animación
        if (Time.timeScale <= 0f)
            return;

        bool moving = false;

        if (_movement.isActiveAndEnabled && _characterController.enabled)
        {
            // solo se comprueba el movimiento en el eje horizontal
            Vector3 horizontalVelocity = _characterController.velocity;
            horizontalVelocity.y = 0f;

            moving = horizontalVelocity.sqrMagnitude > _movementThreshold * _movementThreshold;
        }

        // se tiene en cuenta la estamina del jugador si está corriendo
        bool running = moving && _movement.IsRunning;

        _animator.SetBool(_movingParameter, moving);
        _animator.SetBool(_runningParameter, running);
    }

    private void OnDisable()
    {
        if (_animator == null)
            return;

        _animator.SetBool(_movingParameter, false);
        _animator.SetBool(_runningParameter, false);
        ResetHandActionTriggers();
    }
    #endregion

    #region Animation
    // Ejecuta la animación de coger algo
    public void PlayPickup(bool useLeftHand)
    {
        if (_animator == null || !isActiveAndEnabled || Time.timeScale <= 0f)
            return;

        ResetHandActionTriggers();

        if (useLeftHand)
            _animator.SetTrigger(_pickLeftParameter);
        else
            _animator.SetTrigger(_pickRightParameter);
    }

    // Ejecuta la animación de coger algo con ambas manos
    public void PlayPickupBoth()
    {
        if (_animator == null || !isActiveAndEnabled || Time.timeScale <= 0f)
            return;

        ResetHandActionTriggers();
        _animator.SetTrigger(_pickBothParameter);
    }

    // Ejecuta la animación de soltar algo
    public void PlayDrop(bool useLeftHand)
    {
        if (_animator == null || !isActiveAndEnabled || Time.timeScale <= 0f)
            return;

        ResetHandActionTriggers();

        if (useLeftHand)
            _animator.SetTrigger(_dropLeftParameter);
        else
            _animator.SetTrigger(_dropRightParameter);
    }

    // Reseteo de los triggers de las animaciones
    private void ResetHandActionTriggers()
    {
        _animator.ResetTrigger(_pickLeftParameter);
        _animator.ResetTrigger(_pickRightParameter);
        _animator.ResetTrigger(_pickBothParameter);
        _animator.ResetTrigger(_dropLeftParameter);
        _animator.ResetTrigger(_dropRightParameter);
    }
    #endregion
}
