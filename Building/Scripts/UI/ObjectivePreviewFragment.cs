
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ObjectivePreviewFragment : UdonSharpBehaviour
{
    public TextMeshProUGUI textDisplay;
    void Start()
    {
        
    }

    public void SetData(bool active, Objective objective)
    {
        gameObject.SetActive(active);
        if (!Utilities.IsValid(objective)) return;
        textDisplay.text = objective.GetUUID();
    }
}
