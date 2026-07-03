
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Objective : WorldPropTemplate
{
    public string displayText;
    private Mission _mission;
    internal bool _eligible;
    internal bool _completed;
    void Start()
    {
        
    }

    public void SetMission(Mission mission)
    {
        _mission = mission;
    }

    public void SetEligible(bool value)
    {
        Debug.Log($"[Objective] {name} eligible {value}");
        _eligible = value;
    }

    public void SetCompleted(bool value)
    {
        Debug.Log($"[Objective] {name} completed {value}");
        _completed = value;
    }

    public void ObjectiveComplete()
    {
        if (!Utilities.IsValid(_mission)) return;
        _mission.ObjectiveCompleted(this);
    }
}
