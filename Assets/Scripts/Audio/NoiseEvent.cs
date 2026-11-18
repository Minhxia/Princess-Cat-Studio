using System;
using UnityEngine;

// Guarda los datos de un ruido concreto (pisada) y comunica ese ruido a los enemigos
// que estén escuchando
public struct NoiseEvent
{
    public Vector3 Position;
    public float Radius;        // alcance del ruido (hasta dónde se oye)
    public float Loudness;      // intensidad del ruido
    public GameObject ASource;  // objeto que ha producido el ruido (jugador)
    public NoiseEvent (Vector3 position, float radius, float loudness, GameObject asource)
    {
        Position = position;
        Radius = radius;
        Loudness = loudness;
        ASource = asource;
    }

    // Clase que comunica los avisos de ruido a los enemigos
    public static class NoiseSystem
    {
        public static event Action<NoiseEvent> OnNoiseEmited;
        public static void EmitNoise(Vector3 position, float radius, float loudness, GameObject asource)
        {
            NoiseEvent noise = new NoiseEvent(position,radius,loudness,asource);
            // se emplea ? en caso de que no haya enemigos suscritos
            OnNoiseEmited?.Invoke(noise);
        }

    }
}
