
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Perch : Objective
{
    public GameObject visibleObjects;
    public ParticleSystem encourageParticles;
    public ParticleSystem entryParticles;
    public AudioSource entrySound;
    public ParticleSystem completionParticles;
    public AudioSource completionSound;
    public float timeRequired;
    private bool _perched;
    private float _perchStartTime;
    private bool _timerPending;

    public override void Initialize()
    {
        base.Initialize();
        _perched = false;
    }

    public override void ObjectiveStateChanged()
    {
        base.ObjectiveStateChanged();
        if (Utilities.IsValid(visibleObjects)) visibleObjects.SetActive(_eligible);
        if (_eligible && _perched)
        {
            StartTimer();
        }
        else
        {
            StopTimer();
        }
    }


    public override void OnDroneTriggerEnter(VRCDroneApi drone)
    {
        if (!drone.GetPlayer().isLocal) return;
        StartTimer();
    }

    public override void OnDroneTriggerExit(VRCDroneApi drone)
    {
        if (!drone.GetPlayer().isLocal) return;
        StopTimer();
    }

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (!player.isLocal) return;
        StartTimer();
    }

    public override void OnPlayerTriggerExit(VRCPlayerApi player)
    {
        if (!player.isLocal) return;
        StopTimer();
    }

    private void StartTimer()
    {
        if (!_eligible) return;
        _perched = true;
        _perchStartTime = Time.realtimeSinceStartup;
        _timerPending = true;
        SendCustomEventDelayedSeconds(nameof(CheckTimer), timeRequired);
        EntryEffects(true);
        Debug.Log("StartTimer");
    }

    private void StopTimer()
    {
        _perched = false;
        EntryEffects(false);
        Debug.Log("StopTimer");
    }
    public void CheckTimer()
    {
        Debug.Log("CheckTimer");
        if (!_eligible)
        {
            Debug.Log($"CheckTimer failed for eligible {_eligible}");
            return;
        }
        if (!_perched)
        {
            Debug.Log($"CheckTimer failed for perched {_perched}");
            return;
        }
        if (_perchStartTime + timeRequired > Time.realtimeSinceStartup)
        {
            Debug.Log($"CheckTimer failed for time {_perchStartTime + timeRequired} {Time.realtimeSinceStartup}");
            SendCustomEventDelayedSeconds(nameof(CheckTimer), 0);
            return;
        }
        StopTimer();
        CompletionEffects();
        ReportCompletion();
        Debug.Log("Timer Completed");
    }

    private void EntryEffects(bool value)
    {
        if (value)
        {
            if (Utilities.IsValid(encourageParticles)) encourageParticles.Stop();
            if (Utilities.IsValid(entryParticles)) entryParticles.Play();
            if (Utilities.IsValid(entrySound)) entrySound.Play();
        }
        else
        {
            if (Utilities.IsValid(encourageParticles)) encourageParticles.Play();
            if (Utilities.IsValid(entryParticles)) entryParticles.Stop();
            if (Utilities.IsValid(entrySound)) entrySound.Stop();
        }
    }

    private void CompletionEffects()
    {
        if (Utilities.IsValid(completionParticles)) completionParticles.Play();
        if (Utilities.IsValid(completionSound)) completionSound.Play();
    }
}
