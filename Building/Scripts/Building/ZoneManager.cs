
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

public class ZoneManager : UdonSharpBehaviour
{
    public static ZoneManager Instance()
    {
        GameObject gameObject = GameObject.Find("ZoneManager");
        if (!Utilities.IsValid(gameObject)) return null;
        return gameObject.GetComponent<ZoneManager>();
    }
    public BuildManager buildManager;
    public MenuBarRegistry menuBarRegistry;
    private DataDictionary _zones = new DataDictionary();
    private DataDictionary _currentZone;
    private bool _changesPending;
    public ZoneData CurrentZone => (ZoneData)_currentZone;
    [NonSerialized] public DataToken variable;
    void Start()
    {
    }

    public void ImportProvider(ZoneProvider provider)
    {
        var keys = _zones.GetKeys();
        for (int i = 0; i < keys.Count; i++)
        {
            AddZone(provider.Path(), (ZoneData)_zones[keys[i]].DataDictionary);
        }
    }

    void AddZone(string prefix, ZoneData zone)
    {
        string path = $"{prefix}/{zone.ID()}";
        if (_zones.ContainsKey(path)) return;
        _zones[ path] = zone;
        menuBarRegistry.RegisterMenuItem($"Load/{path}", this, nameof(Load), path);
    }

    public void Load()
    {
        LoadZoneByPath(variable.String);
    }


    public void LoadZoneByPath(string path)
    {
        if (!_zones.TryGetDataDictionary(path, out DataDictionary dictionary)) return;
        ZoneData zone = (ZoneData)dictionary;
        LoadZone(zone);
    }
    public void LoadZone(ZoneData zoneData)
    {
        if (!Utilities.IsValid(zoneData)) return;
        if (Utilities.IsValid(CurrentZone))
        {
            CurrentZone.PropsObservable().ClearSubscription(this); // unsubscribe from previous zone changes
            _currentZone = null;
        }
        _currentZone = zoneData;
        zoneData.PropsObservable().Subscribe(this, nameof(ZoneDataChanged)); // subscribe to new zone changes
    }

    public void ZoneDataChanged()
    {
        if (!Utilities.IsValid(CurrentZone)) return;
        buildManager.LoadSave(CurrentZone.Props());
    }

    public void BuildManagerChanged()
    {
        if (!CurrentZone.CanEdit())
        {
            buildManager.LoadSave(CurrentZone.Props());
            Debug.LogError("BuildManager attempted to modify zone that was not editable, returning to default");
            return;
        }

        if (_changesPending) return; // no need to repeat if something is already coming down the pipeline
        _changesPending = true;
        SendCustomEventDelayedSeconds(nameof(ApplyChangesFromBuildManager), 0);
        //Debug.Log("BuildManager changed, applying changes to zone");
    }

    public void ApplyChangesFromBuildManager()
    {
        CurrentZone.PropsObservable().SetValue(buildManager.ExportSave());
        _changesPending = false;
    }
}
