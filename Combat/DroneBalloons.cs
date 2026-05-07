
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;

public class DroneBalloons : UdonSharpBehaviour
{
    [UdonSynced]
    public int balloonCount;
    public Transform[] balloons;
    public float[] stringLength;
    public float buoyancy;
    public Transform mover;
    public AnimationCurve boundaryForce;
    private VRCDroneApi _ownerDrone;
    

    public static DroneBalloons Instance(VRCPlayerApi playerApi)
    {
        GameObject[] playerObjects = Networking.GetPlayerObjects(playerApi);
        for (int i = 0; i < playerObjects.Length; i++)
        {
            if (!Utilities.IsValid(playerObjects[i])) continue;
            DroneBalloons balloons = playerObjects[i].GetComponentInChildren<DroneBalloons>();
            if (!Utilities.IsValid(balloons)) continue;
            return balloons;
        }
 
        return null;
    }
    private void Start()
    {
        _ownerDrone = Networking.GetOwner(gameObject).GetDrone();
        for (int i = 0; i < balloons.Length; i++)
        {
            var target = balloons[i].GetComponent<Target>();
            target.Subscribe(this);
            if (Networking.IsOwner(gameObject))
            {
                target.active = false;
            }
        }
    }

    public void OnTargetHit()
    {
        SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(DoDamage));
    }

    public void ChangeBalloonCount(int count)
    {
        balloonCount += count;
        if (balloonCount < 0) balloonCount = 0;
        if (balloonCount >= balloons.Length) balloonCount = balloons.Length;
        RequestSerialization();
    }
    public void DoDamage()
    {
        if (!Networking.IsOwner(gameObject)) return;
        balloonCount--;
        if (balloonCount < 0) balloonCount = 0;
        RequestSerialization();
    }
    private void Update()
    {
        if (!_ownerDrone.IsDeployed())
        {
            if (Networking.IsOwner(gameObject)) balloonCount = 0;
            mover.gameObject.SetActive(false);
            return;
        }
        mover.gameObject.SetActive(true);

        for (int i = 0; i < balloons.Length && i < balloonCount; i++)
        {
            // add buoyancy
            balloons[i].position += Vector3.up * buoyancy * Time.deltaTime;
            // push balloons away from eachother
            for (int j = 0; j < balloons.Length && j < balloonCount; j++)
            {
                if (i == j) continue;
                Vector3 difference = balloons[i].position - balloons[j].position;
                float distance = difference.magnitude;
                if (distance < 1)
                {
                    balloons[i].position += difference.normalized * boundaryForce.Evaluate(difference.magnitude) * Time.deltaTime;
                    balloons[j].position -= difference.normalized * boundaryForce.Evaluate(difference.magnitude) * Time.deltaTime;
                }
            }
            // pull with string
            balloons[i].position = _ownerDrone.GetPosition() + Vector3.ClampMagnitude((balloons[i].position - _ownerDrone.GetPosition()), stringLength[i]);
        }
    }

    public override void OnPreSerialization()
    {
        ApplySerialization();
    }

    public override void OnDeserialization(DeserializationResult result)
    {
        ApplySerialization();
    }

    public void ApplySerialization()
    {
        balloons[0].gameObject.SetActive(balloonCount > 0);
        balloons[1].gameObject.SetActive(balloonCount > 1);
        balloons[2].gameObject.SetActive(balloonCount > 2);
    }
}
