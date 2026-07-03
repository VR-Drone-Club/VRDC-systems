
using UdonSharp;
using Unity.Mathematics;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

public class GateProp : WorldPropTemplate
{
    public DroneGate[] gates;
    private bool _initialized;
    private GateConnectorProp[] _connectors = new GateConnectorProp[0];
    
    private void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        foreach (var gate in gates)
        {
            gate.SubscribeProp(this);
        }
    }

    public void SubscribeConnector(GateConnectorProp prop)
    {
        Initialize();
        if (_connectors.Contains(prop)) return; // don't bother subscribing if already subscribed
        //Debug.Log($"connector {prop} subscribed to gateprop {name}");
        _connectors = _connectors.Add(prop);
    }
    public void GateTriggered(DroneGate gate)
    {
        //Debug.Log($"gateprop {name} received trigger from {gate}");
        int index = gates.IndexOf(gate);
        if (index == -1) return;
        foreach (var connector in _connectors) // send to all connectors that have subscribed to this
        {
            if (Utilities.IsValid(connector)) connector.GateTriggered(this, index);
        }
    }
    public override void DeserializeProp(DataDictionary parameters)
    {
        Initialize();
        base.DeserializeProp(parameters);
        GetUUID();
    }

    public override DataDictionary SerializeProp()
    {
        Initialize();
        return base.SerializeProp();
    }

    public int GetGateID(DroneGate gate)
    {
        return gates.IndexOf(gate);
    }

    public DroneGate GetGate(int index)
    {
        return gates[index];
    }
}
