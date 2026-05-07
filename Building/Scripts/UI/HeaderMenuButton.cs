
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class HeaderMenuButton : UdonSharpBehaviour
{
    private TextMeshProUGUI _textDisplay;
    private HeaderMenuBar _bar;
    private string _key;
    private bool _initialized;
    void Start()
    {
        
    }

    public void Initialize()
    {
        if (_initialized) return;
        _textDisplay = GetComponentInChildren<TextMeshProUGUI>(true);
        _bar = GetComponentInParent<HeaderMenuBar>();
        _initialized = true;
    }

    public void SetData(bool active, string key)
    {
        Initialize();
        gameObject.SetActive(active);
        if (!active) return;
        _textDisplay.text = key;
        _key = key;
    }

    public void ButtonPressed()
    {
        Initialize();
        _bar.ButtonPressed(_key);
    }
}
