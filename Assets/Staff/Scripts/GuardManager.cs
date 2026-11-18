using System.Collections.Generic;
using UnityEngine;

public class GuardManager : MonoBehaviour
{
    [SerializeField] private List<GuardFSMRunner> _guards;
    [SerializeField] private Transform[] _doorPoints;
    private GuardFSMRunner _guardChosen;

    void Start() 
    {
        Debug.Log("[GuardManager] Choosing who guard the door...");
        _guardChosen = _guards[Random.Range(0, _guards.Count)].GetComponent<GuardFSMRunner>();
        Debug.Log("[GuardManager] Guard chosen: " + _guardChosen);
        _guardChosen.ReceiveOrderFromManager();
    }

    public void CoordinateShift() => Debug.Log("[GuardManager] Coordinating shift...");
}
