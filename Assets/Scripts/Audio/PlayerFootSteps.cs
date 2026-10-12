using Unity.VisualScripting;
using UnityEngine;

public class PlayerFootSteps : MonoBehaviour 
{
    [Header("Referencias")]
    [SerializeField] private CharacterController movimientoPersonaje;
    [SerializeField] private AudioSource audioSource;

    [Header("Sonidos")]
    [SerializeField] private AudioClip[] walkClips;
    [SerializeField] private AudioClip[] runClips;

    [Header("Ritmo")]
    [SerializeField] float walkInterval = 0.5f;
    [SerializeField] float runInterval = 0.3f;
    [SerializeField] float runSpeed = 4f;

    [Header("Deteccion")]
    [SerializeField] private float walkRadius = 5f;
    [SerializeField] private float runRadius = 12f;

    private float stepTimer;

    private void Update()
    {
        if (movimientoPersonaje == null || audioSource == null) return;

        bool moving = movimientoPersonaje.isGrounded && movimientoPersonaje.velocity.magnitude > 0.1f;

        if (!moving )
        {
            stepTimer = 0f;
            return;
        }

        bool running = movimientoPersonaje.velocity.magnitude >= runSpeed;
        float interval = running ? runInterval : walkInterval;
        stepTimer += Time.deltaTime;

        if(stepTimer >= interval)
        {
            stepTimer = 0f;
            MakeFootStep(running);
        }
    }

    private void MakeFootStep(bool running)
    {
        AudioClip[] clips = running ? runClips : walkClips;

        if(clips != null && clips.Length > 0  )
        {
            AudioClip clip = clips[Random.Range(0, clips.Length)];
            audioSource.PlayOneShot(clip);
        }

        float radius = running ? runRadius : walkRadius;
        float loudness = running ? 1f : 0.4f;
        NoiseEvent.NoiseSystem.EmitNoise(transform.position, radius, loudness, gameObject);
    }
}
