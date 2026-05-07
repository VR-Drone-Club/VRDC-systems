
using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
using VRDC_systems.Building.Scripts.Building;
using VRDC_systems.Building.Scripts.UI;

public class DesktopBuilderPage : UdonSharpBehaviour
{
    public Builder builder;
    public BuildManager buildManager;
    public MenuBarRegistry menuBarRegistry;
    public TextBinding byteCountBinding;
    public TextBinding successBinding;
    public TextBinding cloggedBinding;
    private Toolbar _toolbar;
    private ToolActionBar _toolActionBar;
    private HeaderMenuBar _headerMenuBar;
    private SelectionBox _selectionBox;
    private ToolInspector[] _toolEditors = new ToolInspector[0];
    private bool _initialized;
    void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        _toolbar = GetComponentInChildren<Toolbar>();
        _toolActionBar = GetComponentInChildren<ToolActionBar>();
        _selectionBox = GetComponentInChildren<SelectionBox>();
        _headerMenuBar = GetComponentInChildren<HeaderMenuBar>();
        _headerMenuBar.Bind(menuBarRegistry.Registry, menuBarRegistry.Callback);
        builder.Initialize();
        buildManager = builder.buildManager;
        buildManager.Initialize();
        builder.SelectedTool.Subscribe(this, nameof(SelectedToolChanged));
        byteCountBinding.SetData(buildManager.SerializationByteCount);
        byteCountBinding.SetLabel("Byte Count: ");
        successBinding.SetData(buildManager.SerializationSuccess);
        successBinding.SetLabel("Success: ");
        cloggedBinding.SetData(buildManager.SerializationClogged);
        cloggedBinding.SetLabel("Clogged: ");
    }

    public void RegisterEditor(ToolInspector toolInspector)
    {
        Initialize();
        _toolEditors = _toolEditors.Add(toolInspector);
        SelectedToolChanged();
    }
    public void SelectedToolChanged()
    {
        _toolbar.SetData(builder.SelectedTool, builder.builderTools);
        for (int i = 0; i < _toolEditors.Length; i++)
        {
            if (_toolEditors[i].AssociatedTool() == builder.ActiveTool.name)
            {
                _toolEditors[i].SetActive(true);
                _toolEditors[i].SetData(buildManager, builder, builder.ActiveTool);
            }
            else
            {
                _toolEditors[i].SetActive(false);
            }
        }
        _toolActionBar.SetData(builder.ActiveTool.Actions);
        
        var tool = builder.ActiveTool;

        if (tool.HasProperty("SelectEndPoint"))
        {
            Debug.Log("Tool changed, SelectTool");
            tool.GetProperty("SelectStartPoint").Subscribe(this, nameof(SelectionBoxChanged));
            tool.GetProperty("PrimaryDown").Subscribe(this, nameof(SelectionBoxChanged));
            tool.GetProperty("SelectEndPoint").Subscribe(this, nameof(SelectionBoxChanged));
        }
    }

    public void SelectionBoxChanged()
    {
        Debug.Log("SelectionBoxChanged");
        var tool = (SelectTool)builder.ActiveTool;
        if (!tool.HasProperty("SelectEndPoint")) return;
        Vector3 startPoint = tool.SelectStartPoint.GetVector3();
        Vector3 endPoint = tool.SelectEndPoint.GetVector3();
        bool primaryDown = tool.PrimaryDown.GetBool();
        
        _selectionBox.SetData(primaryDown, startPoint, endPoint);
    }
}
