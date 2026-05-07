
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using VRC.Udon.Common.Enums;
using VRC.Udon.Common.Interfaces;

public class DronePickup : UdonSharpBehaviour
{
    public Transform _holdOffset;
    public bool dropOnCollision = true;
    public float trailScale = 1;
    
    [UdonSynced]
    [FieldChangeCallback(nameof(Held))]
    private bool _syncedHeld;
    private bool _localHeld;
    private bool _ignoreChanges;
    public bool Held
    {
        get
        {
            return _syncedHeld;
        }
        set
        {
            /*
            EventTracker.Instance().TrackEvent(nameof(DronePickup), nameof(Held) + "Changed", gameObject)
                .AddParameter("Old", _syncedHeld)
                .AddParameter("New", value);
            */
            _cooldownTime = Time.realtimeSinceStartup;
            _syncedHeld = value;
            if (_syncedHeld && Networking.IsOwner(gameObject))
            {
                #if UDONSHELL
                UdonShellReferenceManager.Instance().udonShellCore.SendCommand((7453 << 2) * 5, $"playburst @s {UdonShellUtilities.VectorToString(transform.position)} 0,0,0 1", false, false, false, false, false);
                #endif
                //EffectPicker.Instance().Burst(Networking.GetOwner(gameObject), transform.position, transform.rotation);
            }
            RequestSerialization();
            if (_localHeld != _syncedHeld && !_ignoreChanges)
            {
                if (_syncedHeld) Attach(Networking.GetOwner(gameObject));
                else if (!_syncedHeld) Detach();
            }
        }
    }
    private VRCDroneApi _attachedDrone;
    private float _cooldownTime;
    private bool _trailActive;
    private VRC_Pickup _pickup;


    private void Start()
    {
        _pickup = GetComponent<VRC_Pickup>();
    }

    public override void OnDroneTriggerEnter(VRCDroneApi drone)
    {
        /*EventTracker.Instance().TrackEvent(nameof(DroneGrabbable), nameof(OnDroneTriggerEnter), gameObject)
            .AddParameter("Player", drone.GetPlayer())
            .AddParameter("LocalHeld", _localHeld)
            .AddParameter("SyncedHeld", _syncedHeld);*/
        
        if (!drone.GetPlayer().isLocal) return;
        Attach(Networking.LocalPlayer);
    }
    private void Attach(VRCPlayerApi player)
    {
        /*EventTracker.Instance().TrackEvent(nameof(DroneGrabbable), nameof(Attach) + "Start", gameObject)
            .AddParameter("Player", player)
            .AddParameter("LocalHeld", _localHeld)
            .AddParameter("SyncedHeld", _syncedHeld);*/
        
        if (player.isLocal && !Held)
        {
            if (_cooldownTime + 1 > Time.realtimeSinceStartup) return;
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            Held = true;
            OnAttached();
            return;
        }
        gameObject.layer = 22;
        _localHeld = true;
        _attachedDrone = Networking.GetOwner(gameObject).GetDrone();
        
        /*EventTracker.Instance().TrackEvent(nameof(DroneGrabbable), nameof(Attach) + "End", gameObject)
            .AddParameter("Player", player)
            .AddParameter("LocalHeld", _localHeld)
            .AddParameter("SyncedHeld", _syncedHeld);*/
    }

    public virtual void OnAttached(){}
    public virtual void OnDetached(){}
    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        _cooldownTime = Time.realtimeSinceStartup;
        if (_localHeld && _syncedHeld && player.GetDrone().IsDeployed())
        {
            Detach();
            Attach(player);
        }

        if (Utilities.IsValid(_pickup) && _pickup.IsHeld)
        {
            EnableTrail();
        }
        else
        {
            DisableTrail();
        }
    }

    public override void OnPickup()
    {
        SendCustomNetworkEvent(NetworkEventTarget.All, nameof(EnableTrail));
    }

    public void Detach(bool forced = false)
    {
        /*EventTracker.Instance().TrackEvent(nameof(DroneGrabbable), nameof(Detach) + "Start", gameObject)
            .AddParameter("LocalHeld", _localHeld)
            .AddParameter("SyncedHeld", _syncedHeld);*/
        
        if (Held && Networking.IsOwner(gameObject))
        {
            if (_cooldownTime + 3 > Time.realtimeSinceStartup && !forced) return;
            _ignoreChanges = true;
            Held = false;
            _ignoreChanges = false;
            OnDetached();
        }   
        if (!_localHeld) return;
        gameObject.layer = 13;
        _localHeld = false;
        _attachedDrone = null;
        if (Networking.IsOwner(gameObject))
        {
            GetComponent<Rigidbody>().velocity = Vector3.zero;
        }
        
        /*EventTracker.Instance().TrackEvent(nameof(DroneGrabbable), nameof(Detach) + "End", gameObject)
            .AddParameter("LocalHeld", _localHeld)
            .AddParameter("SyncedHeld", _syncedHeld);*/
    }

    private void Update()
    {
        if (!_localHeld) return;
        if (!Utilities.IsValid(_attachedDrone))
        {
            Detach();
            return;
        }
        if (!_attachedDrone.IsDeployed())
        {
            Detach();
            return;
        }
        if (_attachedDrone.GetPlayer() != Networking.GetOwner(gameObject))
        {
            Detach();
            return;
        }

        if (Utilities.IsValid(_holdOffset))
        {
            transform.position = _attachedDrone.GetPosition() + (transform.position - _holdOffset.position);
            transform.rotation = _attachedDrone.GetRotation() * (Quaternion.Inverse(_holdOffset.rotation) * transform.rotation);
            GetComponent<Rigidbody>().velocity = _attachedDrone.GetVelocity(); // This is technically inaccurate if the offset is large
        }
        else
        {
            transform.position = _attachedDrone.GetPosition();
            transform.rotation = _attachedDrone.GetRotation();
            GetComponent<Rigidbody>().velocity = _attachedDrone.GetVelocity();
        }

        if (Networking.IsOwner(gameObject) && _attachedDrone.GetPlayer().isLocal)
        {
            _attachedDrone.SetVelocity(_attachedDrone.GetVelocity() + (Vector3.down * Time.deltaTime * 2));
        }
    }
    
    private void OnCollisionEnter(Collision other)
    {
        /*EventTracker.Instance().TrackEvent(nameof(DroneGrabbable), nameof(OnCollisionEnter), gameObject)
            .AddParameter("LocalHeld", _localHeld)
            .AddParameter("SyncedHeld", _syncedHeld)
            .AddParameter("other", other.gameObject);
        */
        DisableTrail();
        if (!dropOnCollision) return;
        if (!Networking.IsOwner(gameObject)) return;
        if (_localHeld) Detach();
    }


    public override void OnPickupUseUp()
    {
        GetComponent<VRCPickup>().Drop();
    }

    public void EnableTrail()
    {
        EffectPicker.Instance().AssignTrail(Networking.GetOwner(gameObject), transform, trailScale);
    }

    public void DisableTrail()
    {
        EffectPicker.Instance().RemoveTrail(transform);
    }
}
