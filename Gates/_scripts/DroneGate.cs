
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
    public ParticleSystem entryEffects;
    public bool rotateEffectsToVelocity;
    public AudioSource entryAudio;
    public Transform forwardControlPoint;
    public Transform reverseControlPoint;

    private GateConnector _connector;
    private GateProp _subscribedProp;

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

    public void SubscribeProp(GateProp gateProp)
    {
        Debug.Log($"gateprop {gateProp} subscribed to {name}");
        _subscribedProp = gateProp;
    }

    public void SimulateTrigger()
    {
        if (Utilities.IsValid(_connector)) _connector.GateTriggered(this); // Pass events along to the GateConnector, if there is one.
        if (Utilities.IsValid(_subscribedProp)) _subscribedProp.GateTriggered(this);
    }
    public override void OnDroneTriggerEnter(VRCDroneApi drone)
    {
        if (Vector3.Dot(transform.forward, drone.GetVelocity()) < 0) return;
        if (Utilities.IsValid(_connector) && drone.GetPlayer().isLocal) _connector.GateTriggered(this); // Pass events along to the GateConnector, if there is one.
        if (Utilities.IsValid(_subscribedProp) && drone.GetPlayer().isLocal) _subscribedProp.GateTriggered(this);
        if (Utilities.IsValid(entryEffects))
        {
            if (rotateEffectsToVelocity) entryEffects.transform.rotation = Quaternion.LookRotation(drone.GetVelocity());
            var main = entryEffects.main;
            var startSpeed = main.startSpeed;
            if (startSpeed.mode == ParticleSystemCurveMode.TwoConstants)
            {
                startSpeed.constantMax = drone.GetVelocity().magnitude * 2;
            }
            else
            {
                startSpeed = drone.GetVelocity().magnitude * 2;
            }
            main.startSpeed = startSpeed;
            entryEffects.Play();
        }

        if (Utilities.IsValid(entryAudio))
        {
            entryAudio.PlayOneShot(entryAudio.clip, drone.GetPlayer().isLocal ? 1 : 0.2f);
        }
    }
    
}
