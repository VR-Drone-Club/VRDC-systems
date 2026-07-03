
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

public class MissionTool : BuilderTool
{
    public OutlineUtility OutlineUtility;
    public Observable Missions
    {
        get
        {
            if (_missionsObservable == null) _missionsObservable = Observable.Create(_missions);
            return _missionsObservable.AsObservable();
        }
    }

    public Observable SelectedMission
    {
        get
        {
            if (_selectedMissionObservable == null) _selectedMissionObservable = Observable.Create(_selectedMission);
            return _selectedMissionObservable.AsObservable();
        }
    }
    private DataList _missionsObservable;
    private Mission[] _missions = new Mission[0]; // expose this as an observable so the inspector can bind to it
    private DataList _selectedMissionObservable;
    private Mission _selectedMission; // expose this as an observable so the inspector can bind to it
    void Start()
    {
        
    }

    public void CreateNewMission()
    {
        Debug.Log("Creating new mission");
        BuildManager.SpawnPropSynced("Mission", Vector3.zero, Quaternion.identity);
        FindExistingMissions();
    }

    public override void SetToolActive(bool active)
    {
        if (active)
        {
            FindExistingMissions();
            OutlineObjectives();
        }
        else
        {
            ClearOutlines();
        }
        
    }

    public void FindExistingMissions()
    {
        if (!BuildManager.PropPools.TryGetDataList("Mission", out DataList missions)) return;
        if (missions.Count != _missions.Length) _missions = new Mission[missions.Count];
        for (int i = 0; i < missions.Count; i++)
        {
            var missionObject = (GameObject)missions[i].Reference;
            _missions[i] = missionObject.GetComponent<Mission>();
        }
        Missions.SetValue(_missions);
    }

    public void SelectMission(Mission mission)
    {
        Debug.Log($"MissionTool setting mission to {mission} {Utilities.IsValid(mission)}");
        SelectedMission.SetValue(mission);
    }
    
    public override void PrimaryAction(bool down)
    {
        if (!down) return;
        if (!Utilities.IsValid(SelectedMission.GetReference()))
        {
            Debug.Log("Selected mission not active");
            return;
        }
        if (!Builder.Raycast(QueryTriggerInteraction.Collide, out var pos, out var normal, out var gameObject))
        {
            Debug.Log("raycast failed");
            return;
        }
        var found = gameObject.GetComponentInParent<Objective>();
        if (!Utilities.IsValid(found)) 
        {
            Debug.Log("didn't find valid objective");
            return;
        }
        ((Mission)SelectedMission.GetReference()).AddObjective(found);
        Debug.Log($"Added objective {found.GetUUID()} to mission {((Mission)SelectedMission.GetReference()).GetUUID()}");
        OutlineObjectives();
    }

    private DataList outlinedObjectives = new DataList();
    private void OutlineObjectives()
    {
        ClearOutlines();
        if (!Utilities.IsValid(SelectedMission.GetReference())) return;
        var mission = ((Mission)SelectedMission.GetReference());
        for (int i = 0; i < mission.objectives.Length; i++)
        {
            OutlineUtility.AddOutline(mission.objectives[i].gameObject);
            outlinedObjectives.Add(mission.objectives[i].gameObject);
        }
    }

    private void ClearOutlines()
    {
        for (int i = 0; i < outlinedObjectives.Count; i++)
        {
            if (outlinedObjectives[i].IsNull) continue;
            OutlineUtility.RemoveOutline((GameObject)outlinedObjectives[i].Reference);
        }
        outlinedObjectives.Clear();
    }
}