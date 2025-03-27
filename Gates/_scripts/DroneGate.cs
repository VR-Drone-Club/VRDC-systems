
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public enum GateState
{
    Idle,
    EncourageEntry,
    DiscourageEntry,
}
public class DroneGate : UdonSharpBehaviour
{
    public GameObject idleEffects;
    public GameObject encourageEffects;
    public GameObject discourageEffects;
    
    private GateConnector _connector;

    private GateState _state;
    public GateState State
    {
        get
        {
            return _state;
        }
        set
        {
            _state = value;
            if (Utilities.IsValid(encourageEffects)) encourageEffects.SetActive(value == GateState.EncourageEntry);
            if (Utilities.IsValid(discourageEffects)) discourageEffects.SetActive(value == GateState.DiscourageEntry);
            if (Utilities.IsValid(idleEffects)) idleEffects.SetActive(value == GateState.Idle);
        }
    }
    
    public void RegisterConnector(GateConnector connector)
    {
        _connector = connector; // If this script is visible by a GateConnector, it should reach out and tell it where it belongs. This makes that connection happen.
    }
    
    public override void OnDroneTriggerEnter(VRCDroneApi drone)
    {
        if (Utilities.IsValid(_connector)) _connector.GateTriggered(this); // Pass events along to the GateConnector, if there is one.
    }
    
}
