
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class RushwayGate : UdonSharpBehaviour
{
    public Rushway rushway;
    void Start()
    {
        rushway.RegisterGate(this);
    }

    public void _OnShipTriggerEnter()
    {
        rushway.OnShipTriggerEnter(this);
    }
}
