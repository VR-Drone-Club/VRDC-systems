
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

public class MenuBarRegistry : UdonSharpBehaviour
{
    private DataDictionary _registeredItems = new DataDictionary();
    private DataDictionary _registry = new DataDictionary();
    private DataList _registryObservable;
    public Observable Registry
    {
        get
        {
            Initialize();
            return _registryObservable.AsObservable();
        }
    }

    private DataList _callbackObservable;
    public Observable Callback
    {
        get
        {
            Initialize();
            return _callbackObservable.AsObservable();
        }
    }
    private bool _initialized;
    private bool _updateQueued;

    private void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        _registryObservable = Observable.Create(_registry);
        _callbackObservable = Observable.Create(string.Empty);
        Callback.Subscribe(this, nameof(SendCallback));
    }
    void Start()
    {
        RegisterMenuItem("File/New", this, nameof(New));
        RegisterMenuItem("File/Save", this, nameof(Save));
        RegisterMenuItem("File/Save as", null, null);
        
        RegisterMenuItem("Edit/Copy", null, null);
        RegisterMenuItem("Edit/Paste", null, null);
        RegisterMenuItem("Edit/Undo", null, null);
        RegisterMenuItem("Edit/Redo", null, null);
    }

    public void New()
    {
        Debug.Log("Full cycle received NEW!");
    }

    public void Save()
    {
        Debug.Log("Full cycle received SAVE!");
    }
    public void RegisterMenuItem(string key, UdonSharpBehaviour behaviour, string callback)
    {
        Initialize();
        DataList entry = new DataList();
        entry.Add(key);
        entry.Add(behaviour);
        entry.Add(callback);
        _registeredItems[key] = entry;
        QueueUpdate();
    }

    private void QueueUpdate() // wait until next frame to do this because we'll likely get multiple menu items added at once. No need to recalculate the whole registry every time.
    {
        if (_updateQueued) return;
        SendCustomEventDelayedSeconds(nameof(UpdateRegistry), 0);
        _updateQueued = true;
    }
    public void UpdateRegistry()
    {
        _updateQueued = false;
        Initialize();
        _registry.Clear();
        DataList keys = _registeredItems.GetKeys();
        for (int i = 0; i < keys.Count; i++)
        {
            string key = keys[i].String;
            Debug.Log($"Populating registry with entry: {key}");
            string[] splitKey = key.Split('/');
            DataDictionary directory = _registry; // start at the root layer
            for (int j = 0; j < splitKey.Length; j++)
            {
                string subKey = splitKey[j];
                if (j < splitKey.Length - 1) // there's more to this split, find or make a directory
                {
                    if (directory.TryGetDataDictionary(subKey, out DataDictionary dict))
                    {
                        directory = dict; // found existing directory, use that
                    }
                    else
                    {
                        directory[subKey] = new DataDictionary(); // make new directory
                        directory = directory[subKey].DataDictionary;
                    }
                }
                else // end of the split, make the entry
                {
                    directory[subKey] = key; // put the whole string in the end
                }
            }
        }
        Registry.InformSubscribers();
    }

    public void SendCallback()
    {
        if (!_registeredItems.TryGetDataList(Callback.GetString(), out DataList dataList))
        {
            Debug.Log($"Failed to find registered item '{Callback.GetString()}'");
            return;
        }
        var behaviour = (UdonSharpBehaviour)dataList[1].Reference;
        var callback = dataList[2].String;
        if (Utilities.IsValid(behaviour) && !string.IsNullOrEmpty(callback)) behaviour.SendCustomEvent(callback);
        
        //TODO: look through the registry and call the appropriate callback
    }
}
