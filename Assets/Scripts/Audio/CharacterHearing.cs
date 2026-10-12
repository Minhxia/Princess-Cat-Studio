using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class CharacterHearing :MonoBehaviour
{
    [Header("Oyente")]
    [SerializeField, Range(0f, 2f)] private float hearingSensivity = 1f;

    [SerializeField] private LayerMask soundObstacles;
    [Header("Navegacion")]
    [SerializeField] private NavMeshAgent personaje;

    private Vector3 lastHeardPosition;
    private bool hasHeardNoise;

    private void OnEnable()
    {
        NoiseEvent.NoiseSystem.onNoiseEmited += HearNoise;
    }
    private void OnDisable()
    {
        NoiseEvent.NoiseSystem.onNoiseEmited -= HearNoise;
    }

    private void HearNoise(NoiseEvent noise)
    {
        if (noise.ASource == gameObject) return;
        float distance = Vector3.Distance(transform.position, noise.Position);
        float efectiveRadius = noise.Radius * hearingSensivity;
        Vector3 direction = noise.Position - transform.position;
        bool blocked = Physics.Linecast(transform.position + Vector3.up,
                                            noise.Position + Vector3.up,
                                            soundObstacles,
                                            QueryTriggerInteraction.Ignore);

        if (blocked) efectiveRadius *= 0.35f;
        if (distance > efectiveRadius) return;

        Debug.Log(name + "he oido algo.");

        if(personaje != null &&personaje.isOnNavMesh)
        {
            personaje.isStopped = false;
            personaje.SetDestination(lastHeardPosition);
        }

    }
    public bool HasHeardNoise()
    {
        return hasHeardNoise;
    }
}
