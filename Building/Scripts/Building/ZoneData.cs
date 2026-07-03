using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

public class ZoneData : DataDictionary
{
    public static ZoneData Constructor(ZoneProvider zoneProvider, BuildManager buildManager)
    {
        return Constructor(zoneProvider, buildManager.ExportSave());
    }
    public static ZoneData Constructor(ZoneProvider zoneProvider, string props)
    {
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        ZoneData zone = new ZoneData();
#else
        ZoneData zone = (ZoneData)new DataDictionary();
#endif
        zone["props"] = Observable.Create(props);
        zone["id"] = GetNewHash();
        zone["provider"] = zoneProvider;
        return zone;
    }
    private static string GetNewHash()
    {
        System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
        int time = (int)(System.DateTime.UtcNow - epochStart).TotalSeconds;
        return CalculateHash(time.ToString() + Networking.LocalPlayer.displayName + Time.realtimeSinceStartup).ToString();
    }
    private static UInt32 CalculateHash(string read)
    {
        UInt32 hashedValue = 30744573;
        for(int i=0; i < read.Length; i++)
        {
            hashedValue += read[i];
            hashedValue *= 30744573; // scramble 
        }
        return hashedValue;
    }
}

public static class ZoneDataExtensions
{
    public static string ID(this ZoneData zone) => zone["id"].String;
    public static string Props(this ZoneData zone) => zone.PropsObservable().GetString();
    public static Observable PropsObservable(this ZoneData zone) => (Observable)zone["props"].DataList;
    public static ZoneProvider ZoneProvider(this ZoneData zone) => (ZoneProvider)zone["provider"].Reference;
    public static bool CanLoad(this ZoneData zone) => zone.ZoneProvider().CanLoad(zone);
    public static bool CanEdit(this ZoneData zone) => zone.ZoneProvider().CanEdit(zone);
}