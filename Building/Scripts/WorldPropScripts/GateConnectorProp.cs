
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;
using VRDC_systems.Building.Scripts.Building;
using WorldPropScripts;

public class GateConnectorProp : ConnectableProp
{
    public Transform nextGateDisplay;
    private UdonSharpBehaviour[] _subscribedBehaviours = new UdonSharpBehaviour[0];
    private int _nextGate;
    void Start()
    {
        
    }

    public void Subscribe(UdonSharpBehaviour behaviour)
    {
        if (!Utilities.IsValid(behaviour)) return;
        if (_subscribedBehaviours.Contains(behaviour)) return;
        _subscribedBehaviours = _subscribedBehaviours.Add(behaviour);
    }

    /// <summary>
    /// Copy local data into serialized data
    /// </summary>
    /// <returns></returns>
    public override DataDictionary SerializeProp()
    {
        ApplySerialization();
        return base.SerializeProp();
    }

    public override void DeserializeProp(DataDictionary parameters)
    {
        base.DeserializeProp(parameters);
        ApplySerialization();
    }

    private void ApplySerialization()
    {
        for (int i = 0; i < Connections.GetList().Count; i++)
        {
            PropConnection connection = (PropConnection)Connections.GetList()[i].DataDictionary;
            WorldPropTemplate prop = connection.GetProp(BuildManager);
            if (!Utilities.IsValid(prop)) continue;
            GateProp gate = (GateProp)prop;
            gate.SubscribeConnector(this);
        }
        ApplyNextGatePosition();
    }

    private void ApplyNextGatePosition()
    {
        if (Connections.GetList().Count == 0)
        {
            nextGateDisplay.gameObject.SetActive(false);
            return;
        }
        nextGateDisplay.gameObject.SetActive(true);
        PropConnection connection = GetConnection(_nextGate);
        GateProp prop = (GateProp)connection.GetProp(BuildManager);
        nextGateDisplay.position = prop.GetGate(connection.GetInt("gate")).transform.position;
    }

    public void GateTriggered(GateProp gateProp, int gate)
    {
        Debug.Log($"connector {name} received trigger from gate {gateProp} {gate}");
        for (int i = 0; i < Connections.GetList().Count; i++)
        {
            PropConnection connection = (PropConnection)Connections.GetList()[i].DataDictionary;
            if (connection.GetProp(BuildManager) != gateProp) continue; // filter by matching prop
            if (connection.GetInt("gate") != gate) continue; // filter by matching gate
            // The same gate may be connected multiple times. We're intentionally triggering every one of those at once, because figuring out which connection matters is the responsibility of the game mode which is using the gate connector
            ConnectionTriggered(i);
        }
    }

    public override void ConnectionChanged()
    {
        ApplySerialization();
    }

    public void ConnectionTriggered(int connection)
    {
        Debug.Log($"Connection {connection} triggered");
        if (_nextGate == connection)
        {
            _nextGate++;
            if (_nextGate >= Connections.GetList().Count) _nextGate = 0;
        }
        ApplyNextGatePosition();
        for (int i = 0; i < _subscribedBehaviours.Length; i++)
        {
            if (!Utilities.IsValid(_subscribedBehaviours[i])) continue;
            _subscribedBehaviours[i].SetProgramVariable("Connector", this);
            _subscribedBehaviours[i].SetProgramVariable("Connection", connection);
            _subscribedBehaviours[i].SendCustomEvent("ConnectionTriggered");
        }
    }

    public override PropConnection FindConnection(GameObject target)
    {
        if (!Utilities.IsValid(target)) return null;
        DroneGate gate = target.GetComponentInParent<DroneGate>(); // get the specific gate we hit
        GateProp prop = target.GetComponentInParent<GateProp>(); // find the prop that manages it
        Debug.Log($"Checking {target.name} for a gate {gate} and prop {prop}");
        if (!Utilities.IsValid(prop)) return null;
        if (!Utilities.IsValid(gate)) return null;
        var connection = PropConnection.Create(prop); // create a connection
        connection["gate"] = prop.GetGateID(gate); // define which gate we're referring to specifically
        return connection;
    }
}
