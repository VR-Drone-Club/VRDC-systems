
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRDC_systems.Building.Scripts.Building;

public class ConnectionEditor : UdonSharpBehaviour
{
    public TextMeshProUGUI textDisplay;
    public ObservableButton removeButton;
    private Observable remove;
    void Start()
    {
        
    }

    public void SetData(bool active, string text = null, Observable remove = null)
    {
        gameObject.SetActive(active);
        if (!active) return;
        textDisplay.text = text;
        removeButton.SetData(true, remove, true, null, null);
    }
}
