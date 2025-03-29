
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DroneGrabbable : UdonSharpBehaviour
{
    [UdonSynced]
    private int _attachedDroneID = 0;
    private bool _attached;
    private VRCDroneApi _attachedDrone;
    private float _cooldownTime;
    
    public override void OnDroneTriggerEnter(VRCDroneApi drone)
    {
        if (!drone.GetPlayer().isLocal) return;
        if (_attachedDroneID != 0) return; 
        Attach();
    }

    private void Update()
    {
        if (_attachedDroneID == 0)
        {
            Detach();
            return;
        }

        if (_attachedDroneID != Networking.GetOwner(gameObject).playerId)
        {
            Detach();
            return;
        }
        if (!Utilities.IsValid(_attachedDrone))
        {
            VRCPlayerApi player = VRCPlayerApi.GetPlayerById(_attachedDroneID);
            if (!Utilities.IsValid(player)) return;
            if (!Utilities.IsValid(player.GetDrone())) return;
            _attachedDrone = player.GetDrone();
        }

        if (!_attachedDrone.IsDeployed())
        {
            Detach();
            return;
        }
        
        Attach();

        if (_attached) transform.position = _attachedDrone.GetPosition();
    }

    private void Attach()
    {
        if (_attached) return;
        if (_cooldownTime + 2 > Time.realtimeSinceStartup) return;
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        _attachedDroneID = Networking.LocalPlayer.playerId;
        _cooldownTime = Time.realtimeSinceStartup;
        gameObject.layer = 22;
        _attached = true;
        Debug.Log($"Attached {name}");
    }
    public void Detach()
    {
        if (!_attached) return;
        if (_cooldownTime + 2 > Time.realtimeSinceStartup) return;
        gameObject.layer = 13;
        _attachedDroneID = 0;
        GetComponent<Rigidbody>().velocity = Vector3.zero;
        _cooldownTime = Time.realtimeSinceStartup;
        _attached = false;
        Debug.Log($"Detached {name}");
    }
    private void OnCollisionEnter(Collision other)
    {
        if (!Networking.IsOwner(gameObject)) return;
        if (_attachedDroneID != 0)
        {
            Detach();
        }
    }
}
