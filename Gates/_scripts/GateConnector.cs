
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class GateConnector : UdonSharpBehaviour
{
    public DroneGate[] gates;

    private void Start()
    {
        if (!Utilities.IsValid(gates)) return;
        foreach (var gate in gates)
        {
            gate.RegisterConnector(this);
        }
    }

    public void GateTriggered(DroneGate gate)
    {
        if (!Utilities.IsValid(gate)) return;
        int index = Array.IndexOf(gates, gate);
        if (index == -1) return;
    }
}
