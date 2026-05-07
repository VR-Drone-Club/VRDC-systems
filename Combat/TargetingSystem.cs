
using System;
using Phasedragon.AdminUtilities;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Rendering;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

public class TargetingSystem : UdonSharpBehaviour
{
    public AutoGun gun;
    
    [NonSerialized]
    public float range = 100;
    [NonSerialized]
    public float maxAngle = 5;
    [NonSerialized]
    public float lossAngle = 20;
    [NonSerialized]
    public float chargeTime = 0.1f;
    [NonSerialized]
    public float cooldownTime = 0.5f;
    [NonSerialized]
    public float searchRadius = 0.5f;
    [NonSerialized]
    public LayerMask searchLayers;
    [NonSerialized] 
    public AnimationCurve targetingCurve;
    
    [NonSerialized]
    public Target lastTarget;
    [NonSerialized]
    public float targetAcquireTime;
    [NonSerialized]
    public float fireTime;
    [NonSerialized] 
    public float lastActivity;

    private Vector3 _lastTargetPosition;
    [NonSerialized] public Vector3 targetPredictedPosition;
    private Vector3 _targetVelocity;
    private Vector3[] _velocitySamples = new Vector3[10];
    private int _velocitySampleIndex;
    private VRCDroneApi _localDrone;
    private void Start()
    {
        _localDrone = Networking.LocalPlayer.GetDrone();
        QuickMenu.Instance().RegisterFloat("AutoGun/range", this, nameof(range), nameof(Sync), 0, 500);
        QuickMenu.Instance().RegisterFloat("AutoGun/maxAngle", this, nameof(maxAngle), nameof(Sync), 0, 30);
        QuickMenu.Instance().RegisterFloat("AutoGun/chargeTime", this, nameof(chargeTime), nameof(Sync), 0, 2);
        QuickMenu.Instance().RegisterFloat("AutoGun/searchRadius", this, nameof(searchRadius), nameof(Sync), 0, 2);
        QuickMenu.Instance().RegisterFloat("AutoGun/cooldownTime", this, nameof(cooldownTime), nameof(Sync), 0, 2);
    }

    private void ClearSamples()
    {
        for (int i = 0; i < _velocitySamples.Length; i++)
        {
            _velocitySamples[i] = Vector3.zero;
        }
    }
    private void AddVelocitySample(Vector3 velocity)
    {
        _velocitySamples[_velocitySampleIndex] = velocity;
        _velocitySampleIndex++;
        if (_velocitySampleIndex >= _velocitySamples.Length) _velocitySampleIndex = 0;
    }

    private Vector3 SampleRollingAverage()
    {
        Vector3 total = Vector3.zero;
        int count = 0;
        for (int i = 0; i < _velocitySamples.Length; i++)
        {
            if (_velocitySamples[i] == Vector3.zero) continue;
            total += _velocitySamples[i];
            count++;
        }

        if (count == 0) return total;
        return total / count;
    }
    public void AssignGun(AutoGun gun)
    {
        Debug.Log($"Assigned gun {gun.name} to local targeting system");
        this.gun = gun;
        range = gun.range;
        maxAngle = gun.maxAngle;
        lossAngle = gun.lossAngle;
        chargeTime = gun.chargeTime;
        cooldownTime = gun.cooldownTime;
        searchRadius = gun.searchRadius;
        searchLayers = gun.searchLayers;
        targetingCurve = gun.targetingCurve;
    }

    public void Sync()
    {
        gun.range = range;
        gun.maxAngle = maxAngle;
        gun.lossAngle = lossAngle;
        gun.chargeTime = chargeTime;
        gun.cooldownTime = cooldownTime;
        gun.searchRadius = searchRadius;
        gun.searchLayers = searchLayers;
    }

    public void Evaluate()
    {
        LookForTarget();
        if (Utilities.IsValid(lastTarget)) EvaluateTarget();
    }

    private void LookForTarget()
    {
        var camera = VRCCameraSettings.PhotoCamera;
        if (!Physics.SphereCast(camera.Position, searchRadius, camera.Forward, out RaycastHit hitinfo, range, searchLayers, QueryTriggerInteraction.Ignore))
        {
            //Debug.Log("Raycast did not find anything");
            return;
        }

        if (!Utilities.IsValid(hitinfo.collider))
        {
            //Debug.Log("Raycast found invalid collider");
            return;
        }
        Target target = hitinfo.collider.GetComponentInParent<Target>();
        if (target == null || !target.active) return;
        lastActivity = Time.realtimeSinceStartup;
        if (lastTarget == target) return;
        lastTarget = target;
        ClearSamples();
        _lastTargetPosition = lastTarget.transform.position;
        targetAcquireTime = Time.realtimeSinceStartup;
    }

    private void EvaluateTarget()
    {
        var camera = VRCCameraSettings.PhotoCamera;
        Vector3 frameVelocity = (lastTarget.transform.position - _lastTargetPosition) / Time.deltaTime;
        _targetVelocity = Vector3.MoveTowards(_targetVelocity, frameVelocity, 100);
        _lastTargetPosition = lastTarget.transform.position;
        AddVelocitySample(frameVelocity);
        float travelTime = Vector3.Distance(lastTarget.transform.position, camera.Position) / 150f;
        targetPredictedPosition = lastTarget.transform.position + (SampleRollingAverage() * travelTime);
        Vector3 direct = (targetPredictedPosition - camera.Position).normalized;
        float angle = Vector3.Angle(direct, camera.Forward);
        if (angle > maxAngle)
        {
            targetAcquireTime = Time.realtimeSinceStartup;
            return;
        }

        if (angle > lossAngle)
        {
            lastTarget = null;
            return;
        }

        if (targetAcquireTime + chargeTime < Time.realtimeSinceStartup && fireTime + cooldownTime < Time.realtimeSinceStartup)
        {
            gun.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(gun.Fire), camera.Position, direct);
            fireTime = Time.realtimeSinceStartup;
        }

    }
}
