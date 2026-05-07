
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

public class TemplateButton : UdonSharpBehaviour
{
    public TextMeshProUGUI nameDisplay;
    public GameObject selectedBorder;
    private DataList _selectedTemplate;
    private string _templateName;
    public void SetData(bool active, string templateName, bool isSelected, Observable selectedTemplate)
    {
        gameObject.SetActive(active);
        nameDisplay.text = templateName;
        selectedBorder.SetActive(isSelected);
        _templateName = templateName;
        _selectedTemplate = selectedTemplate;
    }
    void Start()
    {
        
    }

    public void ButtonPressed()
    {
        if (Utilities.IsValid(_selectedTemplate)) _selectedTemplate.AsObservable().SetValue(_templateName);
    }
}
