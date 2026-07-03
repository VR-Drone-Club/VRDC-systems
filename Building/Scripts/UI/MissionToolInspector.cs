
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;
using VRDC_systems.Building.Scripts.Building;
using VRDC_systems.Building.Scripts.UI;

public class MissionToolInspector : ToolInspector
{
    public ObservableButton createNewMissionButton;
    public ObservableButton detailsBackButton;
    public MissionDetailsFragment missionDetailsFragment;
    private MissionPreviewFragment[] _missionPreviewFragments;
    private bool _initialized;
    private Observable Missions => _missions.AsObservable();
    private DataList _missions;
    private MissionTool _missionTool;
    private void Initialize()
    {
        if (_initialized) return;
        _missionPreviewFragments = GetComponentsInChildren<MissionPreviewFragment>(true);
        missionDetailsFragment = GetComponentInChildren<MissionDetailsFragment>(true);
        createNewMissionButton.SetData(true, "Create New Mission", null, this, nameof(CreateNewMission));
        detailsBackButton.SetData(false, "Back", null, this, nameof(DetailsBack));
        _initialized = true;
    }
    public override string AssociatedTool()
    {
        return "MissionTool";
    }

    public override void SetData(BuildManager buildManager, Builder desktopBuilder, BuilderTool tool)
    {
        Debug.Log("MissionToolInspector SetData");
        Initialize();
        _missionTool = (MissionTool)tool;
        _missions = _missionTool.Missions;
        _missionTool.Missions.Subscribe(this, nameof(MissionsChanged));
        _missionTool.SelectedMission.Subscribe(this, nameof(MissionsChanged));
    }

    public void MissionsChanged()
    {
        Initialize();
        bool selected = _missionTool.SelectedMission.GetReference() != null;
        Mission[] missions = (Mission[])Missions.GetArray();
        Debug.Log($"MissionToolInspector MissionsChanged started with {missions.Length} missions and selected {selected}");
        for (int i = 0; i < _missionPreviewFragments.Length; i++)
        {
            if (i < missions.Length)
            {
                _missionPreviewFragments[i].SetData(!selected, _missionTool, missions[i]);
            }
            else
            {
                _missionPreviewFragments[i].SetData(false, _missionTool, null);
            }
        }

        if (selected)
        {
            var selectedMission = (Mission)_missionTool.SelectedMission.GetReference();
            missionDetailsFragment.SetData(selected, selectedMission);
        }
        else
        {
            missionDetailsFragment.SetData(selected, null);
        }
        createNewMissionButton.SetData(!selected);
        detailsBackButton.SetData(selected);
        
        Debug.Log($"MissionToolInspector MissionsChanged ended with {missions.Length} missions and selected {selected}");
    }

    public void CreateNewMission()
    {
        Debug.Log("MissionToolInspector CreateNewMission");
        if (!Utilities.IsValid(_missionTool)) return;
        _missionTool.CreateNewMission();
    }

    public void DetailsBack()
    {
        if (!Utilities.IsValid(_missionTool)) return;
        _missionTool.SelectMission(null);
    }
}
