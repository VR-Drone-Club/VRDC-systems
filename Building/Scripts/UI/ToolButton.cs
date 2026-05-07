
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

public class ToolButton : UdonSharpBehaviour
{
    public GameObject selectedBorder;
    public TextMeshProUGUI number;
    public Image iconDisplay;
    private DataList _selectedTool;
    private int _index;
    public void SetData(bool selected, bool active, int index, Sprite icon, Observable selectedTool)
    {
        Debug.Log($"SetData Toolbutton {name} with index {index} active {active}");
        number.text = (index + 1).ToString();
        selectedBorder.SetActive(selected);
        gameObject.SetActive(active);
        iconDisplay.sprite = icon;
        _selectedTool = selectedTool;
        _index = index;
    }

    public void ButtonPressed()
    {
        Debug.Log($"Setting selectedTool to {_index}");
        if (Utilities.IsValid(_selectedTool)) _selectedTool.AsObservable().SetValue(_index);
    }
}
