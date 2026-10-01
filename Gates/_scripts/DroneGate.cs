
using System;
using System.Collections.Generic;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

public enum GateState
{
    Idle,
    EncourageEntry,
    DiscourageEntry,
}
public class DroneGate : Objective
{
    public GameObject idleEffects;
    public GameObject encourageEffects;
    public GameObject discourageEffects;
    public ParticleSystem entryEffects;
    public bool rotateEffectsToVelocity;
    public bool strictEntryDirection;
    public AudioSource entryAudio;
    public Transform forwardControlPoint;
    public Transform reverseControlPoint;

    private GateConnector _connector;
    private GateProp _subscribedProp;
    private bool _allowLoop;
    private bool _waitingForNextLoop;

    private GateState _state;
    public GateState State
    {
        get
        {
            return _state;
        }
        set
        {
            _allowLoop = false;
            _state = value;
            switch (_state)
            {
                case GateState.Idle:
                    if (Utilities.IsValid(idleEffects)) idleEffects.SetActive(true); // activate idle when idle
                    if (Utilities.IsValid(encourageEffects)) encourageEffects.SetActive(false);
                    if (Utilities.IsValid(discourageEffects)) discourageEffects.SetActive(false);
                    break;
                case GateState.DiscourageEntry:
                    if (Utilities.IsValid(idleEffects)) idleEffects.SetActive(false);
                    if (Utilities.IsValid(encourageEffects)) encourageEffects.SetActive(false);
                    if (Utilities.IsValid(discourageEffects)) discourageEffects.SetActive(true); // activate discourage when discouraged
                    break;
                case GateState.EncourageEntry:
                    Debug.Log("EncourageEntry");
                    if (Utilities.IsValid(idleEffects)) idleEffects.SetActive(false);
                    if (Utilities.IsValid(discourageEffects)) discourageEffects.SetActive(false);
                    
                    if (strictEntryDirection) EnsureLoopRuns(); // if needed, check per frame 
                    else if (Utilities.IsValid(encourageEffects)) encourageEffects.SetActive(true); // else, just activate encourage
                    break;
            }
        }
    }

    public void EnsureLoopRuns()
    {
        Debug.Log("EnsureLoopRuns");
        _allowLoop = true;
        QueueNextLoop();
    }
    private void QueueNextLoop()
    {
        Debug.Log("QueueNextLoop");
        if (_waitingForNextLoop) return;
        SendCustomEventDelayedSeconds(nameof(StateLoop), 0);
        _waitingForNextLoop = true;
    }
    
    public void StateLoop()
    {
        Debug.Log("StateLoop");
        _waitingForNextLoop = false;
        if (!_allowLoop) return;
        QueueNextLoop();
        
        
        if (State == GateState.EncourageEntry)
        {
            if (strictEntryDirection)
            {
                if (Vector3.Dot(transform.forward, GetPlayerPosition() - transform.position) < 0)
                {
                    if (Utilities.IsValid(encourageEffects)) encourageEffects.SetActive(true);
                    if (Utilities.IsValid(discourageEffects)) discourageEffects.SetActive(false);
                }
                else
                {
                    if (Utilities.IsValid(encourageEffects)) encourageEffects.SetActive(false);
                    if (Utilities.IsValid(discourageEffects)) discourageEffects.SetActive(true);
                }
            }
            else
            {
                if (Utilities.IsValid(encourageEffects)) encourageEffects.SetActive(true);
                if (Utilities.IsValid(discourageEffects)) discourageEffects.SetActive(false);
            }
        }
        else if (State == GateState.DiscourageEntry)
        {
            if (Utilities.IsValid(encourageEffects)) encourageEffects.SetActive(false);
            if (Utilities.IsValid(discourageEffects)) discourageEffects.SetActive(true);
        }
        else if (State == GateState.Idle)
        {
            if (Utilities.IsValid(encourageEffects)) encourageEffects.SetActive(false);
            if (Utilities.IsValid(discourageEffects)) discourageEffects.SetActive(false);
            if (Utilities.IsValid(idleEffects)) idleEffects.SetActive(true);
        }
    }


    public Vector3 GetPlayerPosition()
    {
        var drone = Networking.LocalPlayer.GetDrone();
        if (Utilities.IsValid(drone) && drone.IsDeployed())
        {
            return drone.GetPosition();
        }

        if (DesktopBuilder.Instance()._active)
        {
            return DesktopBuilder.Instance().CameraPosition();
        }
        
        return Networking.LocalPlayer.GetPosition();
    }
    public void RegisterConnector(GateConnector connector)
    {
        _connector = connector; // If this script is visible by a GateConnector, it should reach out and tell it where it belongs. This makes that connection happen.
    }

    public void SubscribeProp(GateProp gateProp)
    {
        //Debug.Log($"gateprop {gateProp} subscribed to {name}");
        _subscribedProp = gateProp;
    }

    public void SimulateTrigger()
    {
        if (Utilities.IsValid(_connector)) _connector.GateTriggered(this); // Pass events along to the GateConnector, if there is one.
        if (Utilities.IsValid(_subscribedProp)) _subscribedProp.GateTriggered(this);
        ReportCompletion();
    }
    public override void OnDroneTriggerEnter(VRCDroneApi drone)
    {
        if (!drone.GetPlayer().isLocal) return;
        EvaluateEntry(drone.GetPosition(), drone.GetVelocity());
    }

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (!player.isLocal) return;
        EvaluateEntry(player.GetPosition(), player.GetVelocity());
    }

    public void EvaluateEntry(Vector3 position, Vector3 velocity)
    {
        if (strictEntryDirection && Vector3.Dot(transform.forward, velocity) < 0) return;
        if (Utilities.IsValid(_connector)) _connector.GateTriggered(this); // Pass events along to the GateConnector, if there is one.
        if (Utilities.IsValid(_subscribedProp)) _subscribedProp.GateTriggered(this);
        ReportCompletion();
        EntryEffects(velocity);
    }
    private void EntryEffects(Vector3 velocity)
    {
        if (Utilities.IsValid(entryEffects))
        {
            if (rotateEffectsToVelocity) entryEffects.transform.rotation = Quaternion.LookRotation(velocity);
            var main = entryEffects.main;
            var startSpeed = main.startSpeed;
            if (startSpeed.mode == ParticleSystemCurveMode.TwoConstants)
            {
                startSpeed.constantMax = velocity.magnitude * 2;
            }
            else
            {
                startSpeed = velocity.magnitude * 2;
            }
            main.startSpeed = startSpeed;
            entryEffects.Play();
        }
        if (Utilities.IsValid(entryAudio))
        {
            entryAudio.Play();
        }
    }

    public override void Initialize()
    {
        base.Initialize();
        State = GateState.Idle;
    }

    public override void ObjectiveStateChanged()
    {
        base.ObjectiveStateChanged();
        if (_eligible) State = GateState.EncourageEntry;
        else State = GateState.Idle;
    }
}
