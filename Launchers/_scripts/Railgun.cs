
using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

public class Railgun : UdonSharpBehaviour
{
    public Transform attachmentPoint;
    public Animator animator;
    public AnimationCurve chargeCurve;
    
    private VRCPickup attachedPickup;
    private float chargeStartTime;

    private void OnTriggerEnter(Collider other)
    {
        if (!Networking.IsOwner(gameObject)) return;
        if (Utilities.IsValid(attachedPickup)) return;
        if (!Utilities.IsValid(other)) return;
        VRCPickup pickup = other.GetComponent<VRCPickup>();
        if (!Utilities.IsValid(pickup)) return;
        if (!Networking.IsOwner(pickup.gameObject)) return;
        attachedPickup = pickup;
    }

    private void Update()
    {
        if (!Networking.IsOwner(gameObject)) return;
        if (!Utilities.IsValid(attachedPickup)) return;
        if (!Networking.IsOwner(attachedPickup.gameObject))
        {
            attachedPickup = null;
            return;
        }
        attachedPickup.transform.position = attachmentPoint.position;
        attachedPickup.GetComponent<Rigidbody>().velocity = Vector3.zero;
        float chargeProgress = chargeStartTime == 0 ? 0 : (Time.timeSinceLevelLoad - chargeStartTime);
        chargeProgress = Mathf.InverseLerp(0, chargeCurve.length, chargeProgress);
        animator.SetFloat("ChargeProgress", chargeProgress);
    }

    public override void OnPickupUseDown()
    {
        chargeStartTime = Time.timeSinceLevelLoad;
    }

    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        attachedPickup = null;
    }

    public override void OnPickupUseUp()
    {
        if (!Utilities.IsValid(attachedPickup)) return;
        float chargeProgress = chargeStartTime == 0 ? 0 : Time.timeSinceLevelLoad - chargeStartTime;
        chargeStartTime = 0;
        attachedPickup.GetComponent<Rigidbody>().velocity = attachmentPoint.forward * chargeCurve.Evaluate(chargeProgress);
        attachedPickup = null;
        animator.SetFloat("ChargeProgress", 0);
    }
}