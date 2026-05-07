
using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

public class DroneInteractableSwitch : UdonSharpBehaviour
{
    public GameObject[] objectsEnabled;
    public GameObject[] objectsDisabled;

    [UdonSynced]
    public bool state;

    private void Start()
    {
        ApplySerialization();
    }

    public override void Interact()
    {
        Toggle();
    }

    public override void OnDroneTriggerEnter(VRCDroneApi drone)
    {
        if (!drone.GetPlayer().isLocal) return;
        Toggle();
    }

    private void Toggle()
    {
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        state = !state;
        RequestSerialization();
        ApplySerialization();
    }

    public override void OnDeserialization(DeserializationResult result)
    {
        ApplySerialization();
    }

    private void ApplySerialization()
    {
        foreach (var enable in objectsEnabled)
        {
            if (Utilities.IsValid(enable)) enable.SetActive(state);
        }
        foreach (var disable in objectsDisabled)
        {
            if (Utilities.IsValid(disable)) disable.SetActive(!state);
        }
    }
}
