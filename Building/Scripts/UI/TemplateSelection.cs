
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;
using VRDC_systems.Building.Scripts.Building;
using VRDC_systems.Building.Scripts.UI;

public class TemplateSelection : ToolInspector
{
    private TemplateButton[] _templateButtons;
    private bool _initialized;
    private DataList _selectedTemplate;
    private DataList _templates;
    private void Initialize()
    {
        _templateButtons = GetComponentsInChildren<TemplateButton>(true);
        _initialized = true;
    }

    public override string AssociatedTool()
    {
        return "SurfaceBuild";
    }

    public override void SetData(BuildManager buildManager, Builder desktopBuilder, BuilderTool tool)
    {
        Initialize();
        var selectedTemplate = tool.GetProperty("SelectedTemplate");
        _selectedTemplate = selectedTemplate;
        _templates = buildManager.GetTemplates();
        selectedTemplate.Subscribe(this, nameof(SelectedTemplateChanged));
    }

    public void SelectedTemplateChanged()
    {
        for (int i = 0; i < _templateButtons.Length; i++)
        {
            if (i < _templates.Count)
            {
                _templateButtons[i].SetData(true, _templates[i].String, _selectedTemplate.AsObservable().GetString() == _templates[i], _selectedTemplate.AsObservable());
            }
            else
            {
                _templateButtons[i].SetData(false, null, false, null);
            }
        }
    }
}
