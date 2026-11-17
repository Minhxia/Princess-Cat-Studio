using UnityEngine;

public class GuardAlertDebug : MonoBehaviour
{
    public void ReceiveSighting(int roomID, Vector3 playerPosition)
    {
        Debug.Log($"[Alerta recibida] Sala {roomID}, Rick en {playerPosition}", this);
    }
}