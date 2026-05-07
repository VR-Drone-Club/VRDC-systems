
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

public class Toolbar : UdonSharpBehaviour
{
    private ToolButton[] _toolButtons;
    private bool _initialized;

    private void Initialize()
    {
        _toolButtons = GetComponentsInChildren<ToolButton>(true);
        _initialized = true;
    }
    public void SetData(Observable selectedTool, BuilderTool[] tools)
    {
        if (!_initialized) Initialize();
        for (int i = 0; i < _toolButtons.Length; i++)
        {
            _toolButtons[i].SetData(i == selectedTool.GetInt(), i < tools.Length,  i, i < tools.Length ? tools[i].icon : null, selectedTool);
        }
    }
    
    void Start()
    {
        
    }
}
