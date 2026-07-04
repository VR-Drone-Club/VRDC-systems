
using System;
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

    private void Start()
    {
        ObjectiveStateChanged();
    }

    public void SetMission(Mission mission)
    {
        _mission = mission;
    }

    public void SetEligible(bool value)
    {
        Debug.Log($"[Objective] {name} eligible {value}");
        _eligible = value;
        ObjectiveStateChanged();
    }
    
    public void ReportCompletion()
    {
        if (!_eligible) return;
        if (!Utilities.IsValid(_mission)) return;
        _mission.ObjectiveCompleted(this);
    }

    public void SetCompleted(bool value)
    {
        Debug.Log($"[Objective] {name} completed {value}");
        _completed = value;
        ObjectiveStateChanged();
    }

    public override void Initialize()
    {
        base.Initialize();
        _eligible = false;
        _completed = false;
        ObjectiveStateChanged();
    }

    public virtual void ObjectiveStateChanged()
    {
        
    }
}
