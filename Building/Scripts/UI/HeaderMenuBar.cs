
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

public class HeaderMenuBar : UdonSharpBehaviour
{
    private HeaderMenuButton[] _buttons;
    public HeaderMenuBar _subMenu;
    private HeaderMenuBar _parentMenu;
    private DataDictionary _activeItems;
    private DataList _callback;
    private bool _initialized;
    public bool root;
    private void Initialize()
    {
        if (_initialized) return;
        _buttons = GetComponentsInChildren<HeaderMenuButton>(true);
        _initialized = true;
    }
    void Start()
    {
        
    }

    public void Bind(Observable registry, Observable callback)
    {
        _activeItems = registry.GetDictionary();
        _callback = callback;
        registry.Subscribe(this, nameof(RegistryChanged));
    }
    public void RegistryChanged()
    {
        SetData(true, _activeItems, _callback.AsObservable());
    }
    public void SetData(bool active, DataDictionary menuItems, Observable callback)
    {
        Initialize();
        gameObject.SetActive(active);
        _callback = callback;
        if (Utilities.IsValid(_subMenu)) _subMenu.SetData(false, null, _callback.AsObservable());
        if (!Utilities.IsValid(menuItems)) return;
        _activeItems = menuItems;
        var keys = menuItems.GetKeys();
        for (int i = 0; i < _buttons.Length; i++)
        {
            if (i < keys.Count)
            {
                string key = keys[i].ToString();
                _buttons[i].SetData(true, key);
            }
            else
            {
                _buttons[i].SetData(false, null);
            }
        }
    }

    public void ButtonPressed(string key)
    {
        if (!Utilities.IsValid(_activeItems)) return;
        if (!_activeItems.TryGetValue(key, out DataToken value)) return;
        switch (value.TokenType)
        {
            case TokenType.DataDictionary:
                _subMenu.SetData(true, value.DataDictionary, _callback.AsObservable());
                break;
            default:
                if (Utilities.IsValid(_callback))
                {
                    _callback.AsObservable().SetValue(value.ToString());
                }
                break;
        }
        if (!root) SetData(false, null, null);
    }
}
