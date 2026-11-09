using UnityEngine;
using BehaviourAPI.Core;
using BehaviourAPI.UnityToolkit;

public class ShiftChangeAction : UnityAction
{
    private GuardManager _guardManager;

    public ShiftChangeAction() { }

    public override void Start() 
    {
        _guardManager = GameObject.Find("GuardManager").GetComponent<GuardManager>();
    }

    public override Status Update() 
    {
        _guardManager.CoordinateShift();
        Debug.Log("[ShiftChangeAction] Shift changed");
        return Status.Success;
    }
}
