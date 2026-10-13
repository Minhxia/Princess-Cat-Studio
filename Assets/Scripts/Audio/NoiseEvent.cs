using UnityEngine;
using System;

public struct NoiseEvent
{
    public Vector3 Position;
    public float Radius;
    public float Loudness;
    public GameObject ASource;
    public NoiseEvent (Vector3 position, float radius, float loudness, GameObject asource)
    {
        Position = position;
        Radius = radius;
        Loudness = loudness;
        ASource = asource;
    }

    public static class NoiseSystem
    {
        public static event Action<NoiseEvent> onNoiseEmited;
        public static void EmitNoise(Vector3 position, float radius, float loudness, GameObject asource)
        {
            NoiseEvent noise = new NoiseEvent(position,radius,loudness,asource);
            onNoiseEmited.Invoke(noise);
        }

    }
}
