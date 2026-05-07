
using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

public enum LauncherState
{
    Idle,
    Suck,
    Charge,
    Hold,
    Fire,
}
public class Railgun : UdonSharpBehaviour
{
    public Transform attachmentPoint;
    public Animator animator;
    public float maxChargeTime;
    public float minChargeVelocity;
    public float maxChargeVelocity;
    public Collider triggerZone;
    
    private VRCPickup attachedPickup;
    private Rigidbody attachedRigidbody;
    private float chargeStartTime;
    private float lastLaunchTime;

    [UdonSynced(UdonSyncMode.Smooth)]
    private float _chargeProgress;
    [FieldChangeCallback(nameof(syncedState))] [UdonSynced]
    private byte _syncedState;

    private LauncherState _state;

    private byte syncedState
    {
        get => _syncedState;
        set
        {
            if (_syncedState == value) return;
            _syncedState = value;
            _state = (LauncherState)_syncedState;
            animator.SetInteger("State", _syncedState);
            Debug.Log($"Railgun state changed to {_state.ToString()}");
            triggerZone.enabled = false;
            switch (_state)
            {
                case LauncherState.Suck:
                    if (Networking.IsOwner(gameObject)) triggerZone.enabled = true;
                    break;
                case LauncherState.Charge:
                    chargeStartTime = Time.timeSinceLevelLoad;
                    ChargeLoop();
                    break;
                case LauncherState.Hold:
                    HoldLoop();
                    break;
                case LauncherState.Fire:
                    animator.SetTrigger("Fire");
                    Launch();
                    SendCustomEventDelayedSeconds(nameof(Idle), 0.5f);
                    break;
            }
        }
    }

    public void Idle()
    {
        syncedState = 0;
    }
    private void Start()
    {
        UdonShellCore core = UdonShellReferenceManager.Instance().udonShellCore;
        core.RegisterFunction(this, nameof(SetRailgunPower), "Player Manipulation")
            .WithArgument(nameof(maxChargeTime), "number").WithDisplayName("Max Charge Time")
            .WithArgument(nameof(maxChargeVelocity), "number").WithDisplayName("Max Charge Velocity");
    }

    private void Launch()
    {
        if (!Utilities.IsValid(attachedPickup)) return;
        DronePickup dronePickup = attachedPickup.GetComponent<DronePickup>();
        dronePickup.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(DronePickup.EnableTrail));
        if (!Networking.IsOwner(gameObject) || !Utilities.IsValid(attachedRigidbody)) return;
        lastLaunchTime = Time.timeSinceLevelLoad;
        attachedRigidbody.velocity = attachmentPoint.forward * Mathf.Lerp(minChargeVelocity, maxChargeVelocity, _chargeProgress);
        attachedPickup = null;
        attachedRigidbody = null;
        Debug.Log($"Launched with charge {_chargeProgress}");
        _chargeProgress = 0;
    }
    public void SetRailgunPower()
    {
        
    }
    private void OnTriggerEnter(Collider other)
    {
        if (_state != LauncherState.Suck) return;
        if (!Networking.IsOwner(gameObject)) return;
        if (Utilities.IsValid(attachedPickup)) return;
        if (!Utilities.IsValid(other)) return;
        VRCPickup pickup = other.GetComponent<VRCPickup>();
        if (!Utilities.IsValid(pickup)) return;
        VRCObjectSync objectSync = other.GetComponent<VRCObjectSync>();
        if (!Utilities.IsValid(objectSync)) return;
        Networking.SetOwner(Networking.LocalPlayer, other.gameObject);
        attachedPickup = pickup;
        attachedRigidbody = attachedPickup.GetComponent<Rigidbody>();
        syncedState = 2;
    }
    
    public void ChargeLoop()
    {
        if (_state != LauncherState.Charge) return;
        if (!Utilities.IsValid(attachedPickup) || !Networking.IsOwner(attachedPickup.gameObject))
        {
            syncedState = 0;
            return;
        }
        if (!MoveAttachedRigidbody()) return;
        
        _chargeProgress = chargeStartTime == 0 ? 0 : Mathf.InverseLerp(chargeStartTime, chargeStartTime + maxChargeTime, Time.timeSinceLevelLoad);
        
        if (Time.timeSinceLevelLoad > chargeStartTime + maxChargeTime)
        {
            syncedState = 3;
            return;
        }
        SendCustomEventDelayedSeconds(nameof(ChargeLoop), 0);
    }

    public void HoldLoop()
    {
        if (_state != LauncherState.Hold) return;
        if (!Utilities.IsValid(attachedPickup) || !Networking.IsOwner(attachedPickup.gameObject))
        {
            syncedState = 0;
            return;
        }

        if (!MoveAttachedRigidbody()) return;
        
        if (_syncedState == 2 && chargeStartTime + maxChargeTime < Time.timeSinceLevelLoad)
        {
            syncedState = 3;
            return;
        }
        SendCustomEventDelayedSeconds(nameof(HoldLoop), 0);
    }

    private bool MoveAttachedRigidbody()
    {
        if (!Utilities.IsValid(attachedPickup) || attachedPickup.IsHeld) return false;
        if (!Utilities.IsValid(attachedRigidbody)) return false;
        attachedPickup.transform.position = attachmentPoint.position;
        attachedRigidbody.velocity = Vector3.zero;
        return true;
    }
    public override void OnPickupUseDown()
    {
        if (_state == LauncherState.Idle)
        {
            syncedState = 1;
        }
        else if (_state == LauncherState.Hold)
        {
            syncedState = 2;
        }
    }
    


    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        attachedPickup = null;
        attachedRigidbody = null;
        if (Networking.IsOwner(gameObject)) syncedState = 0;
    }

    public override void OnPickupUseUp()
    {
        if (_state == LauncherState.Suck)
        {
            syncedState = 0;
        }
        else if (_state == LauncherState.Charge)
        {
            syncedState = 4;
        }
        else if (_state == LauncherState.Hold && _chargeProgress > 0.2f)
        {
            syncedState = 4;
        }
    }


}