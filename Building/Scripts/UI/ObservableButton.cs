
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

public class ObservableButton : UdonSharpBehaviour
{
    public TextMeshProUGUI textDisplay;
    public Image iconDisplay;
    private DataList _observable;
    private DataToken _value;
    
    public void SetData(bool active, Observable observable, DataToken value, string text, Sprite icon)
    {
        textDisplay.text = text;
        gameObject.SetActive(active);
        iconDisplay.sprite = icon;
        _observable = observable;
        _value = value;
    }

    public void ButtonPressed()
    {
        if (Utilities.IsValid(_observable)) _observable.AsObservable().SetValue(_value);
    }
}
