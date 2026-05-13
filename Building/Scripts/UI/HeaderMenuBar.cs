
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
    private bool _active;
    public bool root;
    private void Initialize()
    {
        if (_initialized) return;
        _buttons = GetComponentsInChildren<HeaderMenuButton>(true);
        if (Utilities.IsValid(_subMenu)) _subMenu.SetParent(this);
        _initialized = true;
    }

    public void SetParent(HeaderMenuBar parent)
    {
        _parentMenu = parent;
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
        _active = active;
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

    public void Disable()
    {
        if (!_active || root) return;
        if (Utilities.IsValid(_subMenu)) _subMenu.Disable();
        SetData(false, null, null);
        if (Utilities.IsValid(_parentMenu)) _parentMenu.Disable();
    }

    public void ButtonPressed(string key)
    {
        if (!Utilities.IsValid(_activeItems)) return;
        if (!_activeItems.TryGetValue(key, out DataToken value)) return;
        Debug.Log($"Menu bar button pressed, entry type was {value.TokenType}");
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
                Disable();
                break;
        }
    }
}
