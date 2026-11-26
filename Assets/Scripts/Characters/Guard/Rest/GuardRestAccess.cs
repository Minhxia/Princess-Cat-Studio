using UnityEngine;
using System.Collections;

// Clase que gestiona todo lo que tiene que hacer el guardia al llegar a la puerta de la
// garita con el panel y acceder a su interior.
public class GuardRestAccess : MonoBehaviour
{
    #region Variables
    [Header("Security room")]
    [SerializeField] private SecurityCard _card;            // tarjeta de seguridad
    [SerializeField] private SecurityCardReader _reader;    // lector del panel de seguridad
    [SerializeField] private SecurityDoor _door;            // puerta de la garita
    [SerializeField] private Transform _readerPoint;        // punto junto al panel
    [SerializeField] private Transform _insidePoint;  // punto interior de la garita justo después de cruzar la puerta
    [SerializeField] private Transform _seatPoint;    // punto donde está la silla interior
    [SerializeField] private Animator _animator;
    [SerializeField] private string _sittingParameter = "Sentarse"; // nombre para la animación
    [SerializeField] private string _standingDrinkParameter = "BeberDePies";
    [SerializeField, Min(0f)] private float _standUpSeconds = 1f;   // tiempo para levantarse de la silla

    [Header("Sitting height")]
    [SerializeField] private Transform _guardModel;         // modelo visual del guardia (NO su padre)
    [SerializeField] private float _seatedLocalY = -0.7f;   // altura para sentarse
    [SerializeField, Min(0f)] private float _sitHeightSeconds = 1f; // tiempo para sentarse a esa altura

    private bool _insideRoom;   // indica si el guardia está dentro de la garita
    private bool _readyToSit;   // indica si ya está colocado correctamente para sentarse
    private bool _isSitting;    // indica si ya está sentado

    private float _standingLocalY;   // altura que tenía antes de sentarse
    private Coroutine _heightRoutine;
    #endregion

    #region Getters
    public bool IsInsideRoom => _insideRoom;
    public bool ReadyToSit => _readyToSit;
    public bool IsSitting => _isSitting;

    // Comprueba si el guardia tiene la tarejta
    public bool HasCard
    {
        get { return _card != null && _card.IsCarriedBy(transform); }
    }

    // Comprueba si el guardia está usando la tarejta
    public bool IsUsingCard
    {
        get
        {
            return _card != null && _card.IsInUse;
        }
    }

    public SecurityDoor Door => _door;
    public Transform ReaderPoint => _readerPoint;
    public Transform InsidePoint => _insidePoint;
    public Transform SeatPoint => _seatPoint;
    public float StandUpSeconds => _standUpSeconds;
    #endregion

    // Gira al guardia hacia el panel de seguridad
    public bool FaceReader()
    {
        if (_reader == null)
            return false;

        // dirección desde el guardia hasta el panel
        Vector3 direction = _reader.transform.position - transform.position;
        direction.y = 0f;

        // si la dirección es demasiado pequeña
        if (direction.sqrMagnitude < 0.0001f)
            return true;

        // orientación hacia el panel
        Quaternion target = Quaternion.LookRotation(direction, Vector3.up);
        // el guardia gira progresivamente
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, target, 180f * Time.deltaTime);

        // indica si ha terminado de girar
        return Quaternion.Angle(transform.rotation, target) <= 0.1f;
    }

    // Pide la lectura de la tarjeta al lector del panel de seguridad
    public bool ReadCard()
    {
        if (_card == null || _reader == null || !HasCard)
            return false;

        // se indica que el guardia va a comenzar a usarla, para bloquearla
        _card.BeginUse();

        // se intenta leer
        bool accepted = _reader.TryRead(_card, transform);
        // y si el lector rechaza la tarjeta, se restaura
        if (!accepted)
            _card.EndUse();

        return accepted;
    }

    // Indica si el guardia está ya en el interior de la garita
    public void SetInsideRoom(bool inside)
    {
        _insideRoom = inside;
    }

    // Gira al guardia para que queda orientado hacia el punto de la silla correspondiente
    public bool FaceSeat(Transform seatPoint)
    {
        // orientación de la silla
        Quaternion target = Quaternion.Euler(0f, seatPoint.eulerAngles.y, 0f);
        // el guardia gira progresivamente
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, 180f * Time.deltaTime);
        // y cuando termina devuelve true
        return Quaternion.Angle(transform.rotation, target) <= 0.1f;
    }

    // Indica si está listo para sentarse (colocado correctamente)
    public void SetReadyToSit(bool ready)
    {
        _readyToSit = ready;
    }

    // Activa o desactiva la animación de sentarse
    public void SetSitting(bool sitting)
    {
        if (_animator != null && !string.IsNullOrEmpty(_sittingParameter))
            _animator.SetBool(_sittingParameter, sitting);

        if (_isSitting == sitting)
            return;

        _isSitting = sitting;

        if (_guardModel == null)
            return;

        // se detiene el ajuste anterior si todavía está en curso
        if (_heightRoutine != null)
        {
            StopCoroutine(_heightRoutine);
            _heightRoutine = null;
        }

        float targetY;
        float duration;

        if (sitting)
        {
            // se guarda la altura que tiene justo antes de sentarse
            _standingLocalY = _guardModel.localPosition.y;

            targetY = _seatedLocalY;
            duration = _sitHeightSeconds;
        }
        else
        {
            // al levantarse, se recupera la altura anterior
            targetY = _standingLocalY;
            duration = _standUpSeconds;
        }

        if (duration <= 0f)
        {
            Vector3 position = _guardModel.localPosition;
            position.y = targetY;
            _guardModel.localPosition = position;
            return;
        }

        _heightRoutine = StartCoroutine(
            AdjustModelHeight(targetY, duration));
    }

    // Activa o desactiva la animación de beber café de pies
    public void SetDrinkingStanding(bool drinking)
    {
        if (_animator != null
            && !string.IsNullOrEmpty(_standingDrinkParameter))
        {
            _animator.SetBool(_standingDrinkParameter, drinking);
        }
    }

    // Cambia progresivamente la altura local del modelo del guardia
    private IEnumerator AdjustModelHeight(float targetY, float duration)
    {
        float startY = _guardModel.localPosition.y;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);

            // se modifica únicamente la altura
            Vector3 position = _guardModel.localPosition;

            // se suaviza el comienzo y el final del movimiento
            position.y = Mathf.SmoothStep(startY, targetY, progress);

            _guardModel.localPosition = position;

            yield return null;
        }

        // se asegura que termina exactamente en la altura indicada
        Vector3 finalPosition = _guardModel.localPosition;
        finalPosition.y = targetY;
        _guardModel.localPosition = finalPosition;

        _heightRoutine = null;
    }
}
