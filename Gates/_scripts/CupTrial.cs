
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class CupTrial : UdonSharpBehaviour
{
    public GateConnector gateConnector;
    
    private bool _active;
    void Start()
    {
        gateConnector.autoStart = false;
    }

    public void BeginRound()
    {
        _active = true;
        gateConnector.BeginCourse();
    }

    public void EndRound()
    {
        
    }
}
