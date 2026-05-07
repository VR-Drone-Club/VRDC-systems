
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;
using VRDC_systems.Building.Scripts.Building;
using WorldPropScripts;

public class ConnectionTool : BuilderTool
{
    public ConnectableProp ConnectingProp;
    public Observable Connections => ConnectingProp.Connections;
    void Start()
    {
        
    }

    public override void PrimaryAction(bool down)
    {
        if (!down) return;
        if (!Builder.Raycast(QueryTriggerInteraction.Collide, out var pos, out var normal, out var gameObject)) return;
        if (!Utilities.IsValid(ConnectingProp))
        {
            ConnectingProp = gameObject.GetComponentInParent<ConnectableProp>();
            if (Utilities.IsValid(ConnectingProp))
            {
                Debug.Log("Found connectable");
                return;
            }
        }
        else
        {
            var connection = ConnectingProp.FindConnection(gameObject);
            if (Utilities.IsValid(connection))
            {
                ConnectingProp.AddConnection(connection);
                Debug.Log($"Connected {ConnectingProp.GetUUID()} with {connection.GetPropUUID()} at position {ConnectingProp.Connections.GetList().Count - 1}");
            }
        }
    }
}
