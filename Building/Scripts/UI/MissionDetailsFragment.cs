
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class MissionDetailsFragment : UdonSharpBehaviour
{
    public TextMeshProUGUI nameDisplay;
    public TextMeshProUGUI descriptionDisplay;
    private ObjectivePreviewFragment[] _objectivePreviewFragments;
    private bool _initialized;
    private Mission _selectedMission;
    void Start()
    {
        
    }

    private void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        _objectivePreviewFragments = GetComponentsInChildren<ObjectivePreviewFragment>();
    }
    public void SetData(bool active, Mission mission)
    {
        Initialize();
        gameObject.SetActive(active);
        if (!active) return;
        mission.Objectives.Subscribe(this, nameof(ObjectivesChanged));
        _selectedMission = mission;
        nameDisplay.text = mission.GetUUID();
        descriptionDisplay.text = $"{mission.objectives.Length} objectives";
        ObjectivesChanged();
    }

    public void ObjectivesChanged()
    {
        if (!Utilities.IsValid(_selectedMission)) return;
        if (!Utilities.IsValid(_selectedMission.objectives)) return;
        for (int i = 0; i < _objectivePreviewFragments.Length; i++)
        {
            if (i < _selectedMission.objectives.Length)
            {
                _objectivePreviewFragments[i].SetData(true, _selectedMission.objectives[i]);
            }
            else
            {
                _objectivePreviewFragments[i].SetData(false, null);
            }
        }
    }
}
