
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

public class ZoneProvider : UdonSharpBehaviour
{
    private ZoneManager _zoneManager;
    private DataDictionary zones;
    private DataList zonesObservable;
    private bool _initialized;
    
    private void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        zones = new DataDictionary();
        zonesObservable = Observable.Create(zones);
        _zoneManager = ZoneManager.Instance();
        _zoneManager.ImportProvider(this);
    }
    public Observable Zones()
    {
        Initialize();
        return (Observable)zonesObservable;
    }
    public string Path()
    {
        return string.Empty;
    }
    internal void AddZone(ZoneData zone)
    {
        Initialize();
        zones[zone.ID()] = zone;
        zonesObservable.AsObservable().InformSubscribers();
    }

    internal ZoneData GetZone(string id)
    {
        if (zones.TryGetValue(id, TokenType.DataDictionary, out DataToken value))
            return (ZoneData)value.DataDictionary;
        return null;
    }
    internal void RemoveZone(string id)
    {
        Initialize();
        zones.Remove(id);
        zonesObservable.AsObservable().InformSubscribers();
    }
}
