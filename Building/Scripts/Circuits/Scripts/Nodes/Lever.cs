
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class Lever : ExternNode
{
    [UdonSynced]
    private bool _leverState;

    public GameObject onObject;
    public GameObject offObject;

    private void Start()
    {
        inputNames = new string[0];
        outputNames = new string[]
        {
            "Lever state",
        };
        Initialize();
        SendProperties();
        ApplyState();
    }

    public override void Interact()
    {
        if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
        _leverState = !_leverState;
        RequestSerialization();
        ApplyState();
    }

    public override void OnDeserialization()
    {
        ApplyState();
    }

    public override void SetInput(int index, float value)
    {
        
    }

    public override float GetOutput(int index)
    {
        return _leverState ? 1f : 0f;
    }

    public void ApplyState()
    {
        onObject.SetActive(_leverState);
        offObject.SetActive(!_leverState);
        _circuitsManager.QueueExternOutput(this, 0);
    }

    public override bool RequireAllInputs()
    {
        return false;
    }
}
