
using System;
using Phasedragon.AdminUtilities;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDK3.Rendering;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;

public class AutoGun : UdonSharpBehaviour
{
    public ParticleSystem particleSystem;
    public float range = 100;
    public float maxAngle = 5;
    public float lossAngle = 20;
    public float chargeTime = 0.1f;
    public float cooldownTime = 0.5f;
    public float searchRadius = 0.5f;
    public LayerMask searchLayers;
    public AnimationCurve targetingCurve;

    private void Start()
    {
        if (Networking.IsOwner(gameObject))
        {
            var module = particleSystem.collision;
            module.sendCollisionMessages = true;
        }
    }

    [NetworkCallable]
    public void Fire(Vector3 position, Vector3 direction)
    {
        particleSystem.transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction));
        particleSystem.Play();
    }
}
