using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

public class ZoneData : DataDictionary
{
    public static ZoneData Constructor(string props)
    {
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        ZoneData zone = new ZoneData();
#else
        ZoneData zone = (ZoneData)new DataDictionary();
#endif
        zone["props"] = props;
        zone["id"] = GetNewHash();
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
    public static void SetData(this ZoneData zone)
    {
        
    }

    public static string ID(this ZoneData zone) => zone["id"].String;
    public static string Props(this ZoneData zone) => zone["props"].String;
}