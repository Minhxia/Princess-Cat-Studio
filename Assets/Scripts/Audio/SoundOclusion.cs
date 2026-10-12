using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent (typeof(AudioLowPassFilter))]
public class SoundOclusion : MonoBehaviour
{
    [Header("Oyente")]
    [SerializeField] private Transform listener;
    [Header("Obstaculos")]
    [SerializeField] private LayerMask oclusionMask;
    [Header("Sonido sin obstaculos")]
    [SerializeField, Range(0f, 1f)] private float clearVolumen = 1f;
    [SerializeField, Range(500f, 22000f)] private float clearCutoff = 22000f;
    [Header("Sonido con obtaculos")]
    [SerializeField, Range(0f, 1f)] private float blockedVolumen = 0.3f;
    [SerializeField, Range(500f, 10000)] private float blockedCutoff = 1200f;

    [Header("Transicion")]
    [SerializeField, Min(0.1f)]
    private float smoothingSpeed = 5f;

    [Header("Distancia Limite")]
    [SerializeField] private float maxDistanceVolume = 20f;

    private AudioSource audioSource;
    private AudioLowPassFilter lowPassFilter;

    private float originalVolume;
    private float targetVolume;
    private float targetCuoff;
    
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        lowPassFilter = GetComponent<AudioLowPassFilter>();
        originalVolume = audioSource.volume;
        targetVolume = clearVolumen;
        targetCuoff = clearCutoff;

        lowPassFilter.cutoffFrequency = clearCutoff;

    }
    private void Start()
    {
        if (listener == null && Camera.main != null)
        {
            listener = Camera.main.transform;
        }
    }

    private void Update()
    {
        if (listener == null) return;

        UpdateOclusion();

        audioSource.volume = Mathf.Lerp(
            audioSource.volume,
            originalVolume * targetVolume,
            Time.deltaTime * smoothingSpeed
            );
        lowPassFilter.cutoffFrequency = Mathf.Lerp(
            lowPassFilter.cutoffFrequency,
            targetCuoff,
            Time.deltaTime * smoothingSpeed
            );
    }

    private void UpdateOclusion()
    {
        Vector3 origin = transform.position;
        Vector3 destination = listener.position;
        float distance = Vector3.Distance(transform.position, listener.position);
        if(distance > maxDistanceVolume )
        {
            targetVolume = 0f;
            targetCuoff = blockedCutoff;
            return;
        }

        bool blocked =  Physics.Linecast(
            origin,
            destination,
            oclusionMask,
            QueryTriggerInteraction.Ignore);

        if(!blocked)
        {
            targetVolume = blockedVolumen;
            targetCuoff = blockedCutoff;
        }else
        {
            targetVolume = clearCutoff;
            targetCuoff = clearCutoff;
        }
    }
}
