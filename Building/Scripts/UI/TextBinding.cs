
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

public class TextBinding : UdonSharpBehaviour
{
    public TextMeshProUGUI valueDisplay;
    private DataList _observable;
    private string _label;
    void Start()
    {
        
    }

    public void SetData(Observable observable)
    {
        _observable = observable;
        observable.Subscribe(this, nameof(ValueChanged));
    }

    public void SetLabel(string label)
    {
        _label = label;
        ValueChanged();
    }
    public void ValueChanged()
    {
        if (string.IsNullOrEmpty(_label)) valueDisplay.text = _observable.AsObservable().GetString();
        else valueDisplay.text = _label + _observable.AsObservable().GetString();
        
    }
}
