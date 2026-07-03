
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class PrivateZones : ZoneProvider
{
    void Start()
    {
        
    }

    public override string Path()
    {
        return "private/" + Networking.GetOwner(gameObject).playerId;
    }

    public override bool CanLoad(ZoneData zone)
    {
        return true; // refactor to restrict access
    }
}
