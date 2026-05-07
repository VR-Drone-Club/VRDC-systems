
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

public class LapDisplay : UdonSharpBehaviour
{
    public Color positiveColor;
    public Color negativeColor;
    public Color neutralColor;
    public TextMeshProUGUI nameDisplay;
    public TextMeshProUGUI trackDisplay;
    public RectTransform splitTemplate;
    private DataList _splits = new DataList();


    public void SetLap(LapRecord lapRecord)
    {
        ClearSplits();
        int count = lapRecord.GetSplitCount();
        for (int i = 0; i < count; i++)
        {
            RectTransform split = GetSplit();
            split.anchoredPosition = new Vector2((float)lapRecord.GetSplit(i) * 10f, 0);
            split.Find("Image").GetComponent<Image>().color = lapRecord.GetSplitType(i) ? positiveColor : negativeColor;
            split.SetSiblingIndex(0);
        }
    }

    
    private void ClearSplits()
    {
        for (int i = 0; i < _splits.Count; i++)
        {
            Transform split = (Transform)_splits[i].Reference;
            DisposeSplit(split);
        }
    }
    private RectTransform GetSplit()
    {
        for (int i = 0; i < _splits.Count; i++)
        {
            RectTransform split = (RectTransform)_splits[i].Reference;
            if (split.localScale == Vector3.zero)
            {
                split.localScale = Vector3.one;
                return split;
            }
        }

        RectTransform instantiated = Instantiate(splitTemplate.gameObject).GetComponent<RectTransform>();
        instantiated.SetParent(splitTemplate.parent);
        instantiated.localScale = Vector3.one;
        instantiated.anchoredPosition3D = Vector3.zero;
        instantiated.localRotation = Quaternion.identity;
        _splits.Add(instantiated);
        return instantiated;
    }

    private void DisposeSplit(Transform split)
    {
        split.localScale = Vector3.zero;
    }
}
