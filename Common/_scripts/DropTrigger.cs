
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DropTrigger : UdonSharpBehaviour
{
    void Start()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!Utilities.IsValid(other)) return;
        DronePickup grab = other.gameObject.GetComponent<DronePickup>();
        grab.Detach();
    }
}
