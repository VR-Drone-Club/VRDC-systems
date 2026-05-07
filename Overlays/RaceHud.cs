
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Rendering;
using VRC.SDKBase;
using VRC.Udon;

public class RaceHud : UdonSharpBehaviour
{
    public Transform mover;
    public GameObject split;
    public Image differenceBackground;
    public TextMeshProUGUI timeDisplay;
    public TextMeshProUGUI differenceDisplay;
    public Color positiveColor;
    public Color negativeColor;
    public Color neutralColor;
    public GameObject personalBest;
    public GameObject lapComplete;
    public LapDisplay lapDisplay;
    public LapLog lapLog;
    
    private float _lastTimeDisplayed;
    private PlayerStats _localPlayerStats;

    public static RaceHud Instance()
    {
        GameObject obj = GameObject.Find(nameof(RaceHud));
        if (!Utilities.IsValid(obj))
        {
            Debug.Log("Failed to find gameobject RaceHud");
            return null;
        }
        RaceHud raceHud = obj.GetComponent<RaceHud>();
        if (!Utilities.IsValid(raceHud))
        {
            Debug.Log("RaceHud object did not have component");
            return null;
        }
        return raceHud;
    }

    private void Start()
    {
        _localPlayerStats = PlayerStats.Find(Networking.LocalPlayer);
    }

    public override void PostLateUpdate()
    {
        if (!Networking.LocalPlayer.GetDrone().IsDeployed())
        {
            mover.gameObject.SetActive(false);
            return;
        }
        mover.gameObject.SetActive(true);
        mover.position = VRCCameraSettings.PhotoCamera.Position;
        mover.rotation = VRCCameraSettings.PhotoCamera.Rotation;
    }

    public void DisplaySplit(string hash, LapRecord current)
    {
        if (Utilities.IsValid(lapDisplay)) lapDisplay.SetLap(current);
        if (Utilities.IsValid(lapLog) && current.GetCompleted()) lapLog.NewEntry(current);
        LapRecord best = _localPlayerStats.GetBestLap(hash);
        _lastTimeDisplayed = Time.realtimeSinceStartup;
        split.gameObject.SetActive(true);
        SendCustomEventDelayedSeconds(nameof(CheckSplitDespawn), 5);
        timeDisplay.text = current.GetTime().ToString("N3");
        if (best == null)
        {
            differenceDisplay.text = "--";
            differenceBackground.color = neutralColor;
            return;
        }
        int currentIndex = current.GetSplitCount() - 1;
        double currentTime = current.GetTime();
        double bestTime = best.GetSplit(currentIndex);
        double difference = currentTime - bestTime;
        if (difference > 0)
        {
            differenceDisplay.text = "+" + difference.ToString("N3");
            differenceBackground.color = negativeColor;
        }
        else
        {
            differenceDisplay.text = difference.ToString("N3");
            differenceBackground.color = positiveColor;
        }

        EventTracker.Instance().TrackEvent(nameof(RaceHud), nameof(DisplaySplit), gameObject)
            .AddParameter("CurrentIndex", currentIndex)
            .AddParameter("Difference", difference)
            .AddParameter("BestTime", bestTime)
            .AddParameter("currentLapRecord", current.DeepClone())
            .AddParameter("bestLapRecord", best.DeepClone());
    }

    public void CheckSplitDespawn()
    {
        if (_lastTimeDisplayed + 4 < Time.realtimeSinceStartup) split.gameObject.SetActive(false);
    }
}
