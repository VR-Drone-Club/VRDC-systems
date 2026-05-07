
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class LapLog : UdonSharpBehaviour
{
    public RectTransform template;

    public void NewEntry(LapRecord lapRecord)
    {
        RectTransform instantiated = Instantiate(template.gameObject).GetComponent<RectTransform>();
        instantiated.SetParent(template.parent);
        instantiated.localScale = Vector3.one;
        instantiated.anchoredPosition3D = Vector3.zero;
        instantiated.localRotation = Quaternion.identity;
        LapDisplay lapDisplay = instantiated.GetComponent<LapDisplay>();
        lapDisplay.SetLap(lapRecord);
    }
}
