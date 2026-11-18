using UnityEngine;
using UnityEngine.AI; // para poder usar NavMeshAgent

[DisallowMultipleComponent]

[RequireComponent(typeof(NavMeshAgent))]

// Clase que recibe los avisos de ruido. Comprueba si el enemigo puede oír algún ruido,
// y, si es así, guarda la posición del ruido y le indica que se acerque.
public class CharacterHearing :MonoBehaviour
{
    #region Variables
    [Header("Listener")]
    [SerializeField, Range(0f, 2f)] private float _hearingSensivity = 1f;
    /* Sensibilidad - Resultado
            0           no detecta ruidos
            0.5         detecta hasta la mitad del alcance dado
            1           mantiente el alcance dado
            2           detecta hasta el doble del alcance dado */
    
    [Header("Navigation")]
    [SerializeField] private LayerMask _soundObstaclesLayer;
    
    private NavMeshAgent  _characterAgent;
    private Vector3 _lastHeardPosition;
    private bool _hasHeardNoise;
    #endregion
    
    // TODO: repasar si esto es útil cuando tengamos los comportamientos de los enemigos
    public Vector3 LastHeardPosition => _lastHeardPosition;
    public bool HasHeardNoise => _hasHeardNoise;

    #region Methods
    public void ClearHeardNoise()
    {
        _hasHeardNoise = false;
    }

    private void Awake()
    {
         _characterAgent = GetComponent<NavMeshAgent>();
    }

    // Suscripción al sistema de ruido
    private void OnEnable()
    {
        NoiseEvent.NoiseSystem.OnNoiseEmited += HearNoise;
    }
    // Desuscripción al sistema de ruido
    private void OnDisable()
    {
        NoiseEvent.NoiseSystem.OnNoiseEmited -= HearNoise;
    }

    private void HearNoise(NoiseEvent noise)
    {
        if (noise.ASource == gameObject || noise.Radius <= 0f || noise.Loudness <= 0f) 
            return;

        // se calcula la distancia y el alcance del ruido
        float distance = Vector3.Distance(transform.position, noise.Position);
        float efectiveRadius = noise.Radius * _hearingSensivity;
        // si el ruido ya está demasiado lejos, se descarta el aviso
        if (efectiveRadius <= 0f || distance > efectiveRadius) return;

        // se comprueba si hay un obstáculo entre el personaje y el ruido
        bool blocked = Physics.Linecast(
            transform.position + Vector3.up,    // punto de inicio, donde está el personaje
            noise.Position + Vector3.up,        // punto final, origen del ruido
            _soundObstaclesLayer,               // capa de obstáculos
            QueryTriggerInteraction.Ignore
        );

        // en caso de que haya un obstáculo, se reduce el alcance
        if (blocked)
            efectiveRadius *= 0.35f;
        // y se vuelve a comprobar la distancia
        if (distance > efectiveRadius) return;

        // se guarda la posición del ruido
        _lastHeardPosition = noise.Position;
        _hasHeardNoise = true;
        Debug.Log("[CharacterHearing] " + this.name + " he oído un ruido!");
        // y se le pide que se mueva hacia él
        if( _characterAgent != null && _characterAgent.isActiveAndEnabled 
                && _characterAgent.isOnNavMesh)
        {
             _characterAgent.isStopped = false;
             _characterAgent.SetDestination(_lastHeardPosition);
        }

    }
    #endregion
}
