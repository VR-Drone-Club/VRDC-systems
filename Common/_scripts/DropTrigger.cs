
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

public class DropTrigger : UdonSharpBehaviour
{
    public ParticleSystem particles;
    void Start()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!Utilities.IsValid(other) || !Networking.IsOwner(other.gameObject)) return;
        DronePickup grab = other.gameObject.GetComponent<DronePickup>();
        if (Utilities.IsValid(grab))
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Effects));
            grab.Detach(true);
        }
        
    }

    public void Effects()
    {
        if (Utilities.IsValid(particles)) particles.Play();
    }
}
