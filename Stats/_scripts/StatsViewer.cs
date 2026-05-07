    
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class StatsViewer : UdonSharpBehaviour
{
    public Transform template;
    private GameObject _entry;
    private TextMeshProUGUI _nameText;
    private TextMeshProUGUI _bestTimeText;
    private bool _initialized;
    private void OnEnable()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        _entry = Instantiate(template.gameObject);
        _entry.transform.SetParent(template.parent);
        _entry.transform.localPosition = Vector3.zero;
        _entry.transform.localRotation = Quaternion.identity;
        _entry.transform.localScale = Vector3.one;
        _nameText = _entry.transform.Find("Name").GetComponent<TextMeshProUGUI>();
        _bestTimeText = _entry.transform.Find("BestTime").GetComponent<TextMeshProUGUI>();
        _nameText.text = Networking.GetOwner(gameObject).displayName;
        _bestTimeText.text = "Not set";
    }

    private void OnDestroy()
    {
        if (Utilities.IsValid(_entry)) Destroy(_entry);
    }

    public void SetData(string name, double bestTime)
    {
        _entry.SetActive(true);
        if (Utilities.IsValid(_nameText)) _nameText.text = name;
        if (Utilities.IsValid(_bestTimeText)) _bestTimeText.text = bestTime.ToString("N3");
    }
}
