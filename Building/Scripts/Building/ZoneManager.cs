
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
    public DataDictionary zones;
    [NonSerialized] public DataToken variable;
    void Start()
    {
    }

    public void ImportProvider(ZoneProvider provider)
    {
        var zones = provider.Zones().GetDictionary();
        var keys = zones.GetKeys();
        for (int i = 0; i < keys.Count; i++)
        {
            ImportZone(provider.Path(), (ZoneData)zones[keys[i]].DataDictionary);
        }
    }

    void ImportZone(string prefix, ZoneData zone)
    {
        string path = $"{prefix}/{zone.ID()}";
        if (zones.ContainsKey(path)) return;
        zones[ path] = zone;
        menuBarRegistry.RegisterMenuItem($"Load/{path}", this, nameof(Load), path);
    }

    public void Load()
    {
        if (!zones.TryGetDataDictionary(variable, out DataDictionary dictionary)) return;
        ZoneData zone = (ZoneData)dictionary;
        buildManager.LoadSaveSynced(zone.Props());
    }
}
