
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Collectable : Objective
{
    public GameObject visibleObjects;
    public ParticleSystem entryParticles;
    public AudioSource entrySound;
    public override void ObjectiveStateChanged()
    {
        base.ObjectiveStateChanged();
        if (Utilities.IsValid(visibleObjects)) visibleObjects.SetActive(_eligible);
    }

    public override void OnDroneTriggerEnter(VRCDroneApi drone)
    {
        if (!_eligible) return;
        if (!drone.GetPlayer().isLocal) return;
        base.OnDroneTriggerEnter(drone);
        ReportCompletion();
        EntryEffects();
    }

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (!_eligible) return;
        if (!player.isLocal) return;
        base.OnPlayerTriggerEnter(player);
        ReportCompletion();
        EntryEffects();
    }

    private void EntryEffects()
    {
        if (Utilities.IsValid(entryParticles)) entryParticles.Play();
        if (Utilities.IsValid(entrySound)) entrySound.Play();
    }
}
