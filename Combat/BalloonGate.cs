
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class BalloonGate : UdonSharpBehaviour
{
    public int balloonChange;
    public override void OnDroneTriggerEnter(VRCDroneApi drone)
    {
        if (drone.GetPlayer() != Networking.LocalPlayer) return;
        DroneBalloons.Instance(Networking.LocalPlayer).ChangeBalloonCount(balloonChange);
    }
}
