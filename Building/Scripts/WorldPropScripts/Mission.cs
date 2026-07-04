
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

public class Mission : WorldPropTemplate
{
    private DataList _select;

    public Observable Objectives
    {
        get
        {
            if (_objectivesObservable == null) _objectivesObservable = Observable.Create(objectives);
            return _objectivesObservable.AsObservable();
        }
    }

    private DataList _objectivesObservable;
    public Objective[] objectives;
    public bool autoStart = true;
    private bool[] _objectiveStatus;
    void Start()
    {
        Initialize();
    }

    public override void Initialize()
    {
        foreach (var objective in objectives)
        {
            if (!Utilities.IsValid(objective)) continue;
            objective.SetEligible(false);
            objective.SetCompleted(false);
            objective.SetMission(this);
        }
        if (autoStart) MissionStart();
    }

    public void MissionStart()
    {
        Debug.Log($"[Mission] {name} started");
        _objectiveStatus = new bool[objectives.Length];
        foreach (var objective in objectives)
        {
            objective.SetCompleted(false);
            objective.SetEligible(true);
            objective.SetMission(this);
        }
    }

    public void ObjectiveCompleted(Objective objective)
    {
        int index = Array.IndexOf(objectives, objective);
        if (index == -1) return;
        objective.SetEligible(false);
        objective.SetCompleted(true);
        _objectiveStatus[index] = true;
        CheckMissionCompleted();
    }

    public void CheckMissionCompleted()
    {
        foreach (var status in _objectiveStatus)
        {
            if (!status) return;
        }
        MissionCompleted();
    }
    public void MissionCompleted()
    {
        Debug.Log($"[Mission] {name} completed");
        if (autoStart) MissionStart();
    }

    public override DataDictionary SerializeProp()
    {
        DataList serializedObjectives = GetListParameter("objectives");
        serializedObjectives.Clear();
        foreach (var objective in objectives)
        {
            string uuid = objective.GetUUID();
            serializedObjectives.Add(uuid);
        }
        SetListParameter("objectives", serializedObjectives);
        return base.SerializeProp();
    }

    public override void DeserializeProp(DataDictionary parameters)
    {
        DataList serializedObjectives = GetListParameter("objectives");
        if (objectives.Length != serializedObjectives.Count) objectives = new Objective[serializedObjectives.Count];
        for (int i = 0; i < serializedObjectives.Count; i++)
        {
            var prop = BuildManager.GetPropByUUID(serializedObjectives[i].String);
            if (!Utilities.IsValid(prop)) continue;
            objectives[i] = (Objective)prop; // assume it's an objective, but this is fragile
        }
        if (autoStart) MissionStart();
        base.DeserializeProp(parameters);
    }

    public void AddObjective(Objective objective)
    {
        objectives = objectives.Add(objective);
        Objectives.SetValue(objectives);
        Dirty();
    }
}
