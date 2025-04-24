
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DroneFollower : UdonSharpBehaviour
{
    public Transform followObject;
    private VRCDroneApi _drone;
    void Start()
    {
        _drone = Networking.GetOwner(gameObject).GetDrone();
        Loop();
    }

    public void Loop()
    {
        if (!Utilities.IsValid(_drone)) return;
        if (!_drone.IsDeployed())
        {
            SendCustomEventDelayedSeconds(nameof(Loop), 1);
            followObject.gameObject.SetActive(false);
            return;
        }
        followObject.gameObject.SetActive(true);
        SendCustomEventDelayedSeconds(nameof(Loop), 0);
        followObject.SetPositionAndRotation(_drone.GetPosition(), Quaternion.LookRotation(_drone.GetVelocity()));
    }
}
