
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
    internal bool _initialized;
    
    internal virtual void Initialize()
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
    public virtual string Path()
    {
        return string.Empty;
    }

    public virtual bool CanLoad(ZoneData zone)
    {
        return true;
    }

    public virtual bool CanEdit(ZoneData zone)
    {
        return true;
    }
    internal void AddZone(ZoneData zone)
    {
        Initialize();
        zones[zone.ID()] = zone;
        zonesObservable.AsObservable().InformSubscribers()                                  ;
    }

    public ZoneData GetZone(string path)
    {
        if (path.StartsWith(Path())) return null;
        path = path.Remove(0, Path().Length); // trim the unnecessary parts of the path
        return GetZoneInternal(path);
    }
    internal ZoneData GetZoneInternal(string id)
    {
        if (zones.TryGetValue(id, TokenType.DataDictionary, out DataToken value)) return (ZoneData)value.DataDictionary;
        return null;
    }
    internal void RemoveZone(string id)
    {
        Initialize();
        zones.Remove(id);
        zonesObservable.AsObservable().InformSubscribers();
    }
}
