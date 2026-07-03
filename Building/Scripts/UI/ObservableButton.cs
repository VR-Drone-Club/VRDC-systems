
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

    public void SetData(bool active)
    {
        gameObject.SetActive(active);
    }
    public void SetData(bool active, string text, Sprite icon, UdonSharpBehaviour behaviour, string eventName)
    {
        var observable = Observable.Create(new DataToken());
        observable.Subscribe(behaviour, eventName);
        SetData(active, observable, new DataToken(), text, icon);
    }
    public void SetData(bool active, Observable observable, DataToken value, string text, Sprite icon)
    {
        gameObject.SetActive(active);
        if (Utilities.IsValid(textDisplay)) textDisplay.text = text;
        if (Utilities.IsValid(iconDisplay)) iconDisplay.sprite = icon;
        _observable = observable;
        _value = value;
    }

    public void ButtonPressed()
    {
        if (Utilities.IsValid(_observable)) _observable.AsObservable().SetValue(_value);
    }
}
