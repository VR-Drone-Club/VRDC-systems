
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public enum TrailSource
{
    Owner,
    Local,
}
public class TrailApplicator : UdonSharpBehaviour
{
    public TrailSource sourcePlayer;
    public float size = 1;
    private EffectPicker _effectPicker;
    

    private void Start()
    {
        _effectPicker = EffectPicker.Instance();
        _effectPicker.AssignTrail(sourcePlayer == TrailSource.Local ? Networking.LocalPlayer : Networking.GetOwner(gameObject), transform, size);
    }
}
