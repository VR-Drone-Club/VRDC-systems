
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;
using VRDC_systems.Building.Scripts.Building;

public class ToolActionBar : UdonSharpBehaviour
{
    private ToolActionButton[] _toolActionButtons;
    private bool _initialized;

    private void Initialize()
    {
        _toolActionButtons = GetComponentsInChildren<ToolActionButton>(true);
        _initialized = true;
    }
    public void SetData(DataList toolActions)
    {
        if (!_initialized) Initialize();
        for (int i = 0; i < _toolActionButtons.Length; i++)
        {
            if (i < toolActions.Count) _toolActionButtons[i].SetData(toolActions[i].DataDictionary.AsToolAction());
            else _toolActionButtons[i].SetData(null);
        }
    }
    
    void Start()
    {
        
    }
}
