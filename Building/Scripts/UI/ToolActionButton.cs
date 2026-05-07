
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;
using VRDC_systems.Building.Scripts.Building;

public class ToolActionButton : UdonSharpBehaviour
{
    public Image iconDisplay;
    public DataDictionary action;
    
    public void SetData(ToolAction toolAction)
    {
        action = toolAction;
        if (toolAction == null)
        {
            gameObject.SetActive(false);
            return;
        }
        gameObject.SetActive(true);
        if (Utilities.IsValid(toolAction.Icon())) iconDisplay.sprite = toolAction.Icon();
    }

    public void ButtonPressed()
    {
        action.AsToolAction().InformSubscribers();
    }
}
