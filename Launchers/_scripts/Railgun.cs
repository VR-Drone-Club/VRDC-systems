
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
    public AudioSource audioSource;
    public AudioClip chargeSound;
    public AudioClip launchSound;
    public AudioClip launchSound2;
    public float maxChargeTime;
    public float minChargeVelocity;
    public float maxChargeVelocity;
    
    private VRCPickup attachedPickup;
    private float chargeStartTime;
    private float lastLaunchTime;

    private void OnTriggerEnter(Collider other)
    {
        if (!Networking.IsOwner(gameObject)) return;
        if (Utilities.IsValid(attachedPickup)) return;
        if (!Utilities.IsValid(other)) return;
        VRCPickup pickup = other.GetComponent<VRCPickup>();
        if (!Utilities.IsValid(pickup)) return;
        if (!Networking.IsOwner(pickup.gameObject)) return;
        if (lastLaunchTime + 0.5f > Time.timeSinceLevelLoad) return;
        attachedPickup = pickup;
    }
    
    private void Update()
    {
        if (!Networking.IsOwner(gameObject))
        {
            audioSource.clip = null;
            return;
        }

        if (!Utilities.IsValid(attachedPickup))
        {
            audioSource.clip = null;
            return;
        }
        if (!Networking.IsOwner(attachedPickup.gameObject))
        {
            audioSource.clip = null;
            attachedPickup = null;
            return;
        }
        attachedPickup.transform.position = attachmentPoint.position;
        attachedPickup.GetComponent<Rigidbody>().velocity = Vector3.zero;
        float chargeProgress = chargeStartTime == 0 ? 0 : Mathf.InverseLerp(chargeStartTime, chargeStartTime + maxChargeTime, Time.timeSinceLevelLoad);
        animator.SetFloat("ChargeProgress", chargeProgress);
        audioSource.volume = Mathf.InverseLerp(1, 0.8f, chargeProgress); //should have full volume for most of it, then fade quickly at the end
    }
    
    public override void OnPickupUseDown()
    {
        chargeStartTime = Time.timeSinceLevelLoad;
        audioSource.clip = chargeSound;
        audioSource.volume = 1;
        audioSource.Play();
    }
    
    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        attachedPickup = null;
    }

    public override void OnPickupUseUp()
    {
        if (!Utilities.IsValid(attachedPickup)) return;
        float chargeProgress = chargeStartTime == 0 ? 0 : Mathf.InverseLerp(chargeStartTime, chargeStartTime + maxChargeTime, Time.timeSinceLevelLoad);
        chargeStartTime = 0;
        lastLaunchTime = Time.timeSinceLevelLoad;
        attachedPickup.GetComponent<Rigidbody>().velocity = attachmentPoint.forward * Mathf.Lerp(minChargeVelocity, maxChargeVelocity, chargeProgress);
        attachedPickup = null;
        animator.SetFloat("ChargeProgress", 0);
        audioSource.volume = 1;
        audioSource.PlayOneShot(maxChargeTime < 10 ? launchSound : launchSound2, chargeProgress + 0.5f);
    }
}