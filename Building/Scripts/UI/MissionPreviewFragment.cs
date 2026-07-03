
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

public class MissionPreviewFragment : UdonSharpBehaviour
{
    public TextMeshProUGUI _nameDisplay;
    public TextMeshProUGUI _descriptionDisplay;
    public ObservableButton _detailsButton;
    private MissionTool _missionTool;
    private Mission _mission;
    void Start()
    {
        
    }

    public void SetData(bool active)
    {
        gameObject.SetActive(active);
    }
    public void SetData(bool active, MissionTool missionTool, Mission mission)
    {
        _missionTool = missionTool;
        _mission = mission;
        gameObject.SetActive(active);
        if (!active) return;
        _nameDisplay.text = mission.GetUUID();
        _descriptionDisplay.text = $"{mission.objectives.Length} objectives";
        _detailsButton.SetData(true, "", null, this, nameof(Details));
    }

    public void Details()
    {
        if (!Utilities.IsValid(_mission)) return;
        if (!Utilities.IsValid(_missionTool)) return;
        Debug.Log($"MissionPreviewFragment Details {_mission.name}");
        _missionTool.SelectMission(_mission);
    }
}
