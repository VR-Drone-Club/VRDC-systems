using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDK3.Data;

public class GateLap : DataDictionary
{
    public static GateLap New(int repeat)
    {
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        GateLap lap = new GateLap();
#else
        GateLap lap = (GateLap)new DataDictionary();
#endif
        lap.Add("Repeat", repeat);
        lap.Add("Entries", new DataList());
        return lap;
    }
}

public static class GateLapExtensions
{
    public static int GetRepeat(this GateLap lap) { return lap["Repeat"].Int; }

    public static bool HasChildren(this GateLap lap, int index) { return lap["Entries"].DataList[index].TokenType == TokenType.DataDictionary; }

    public static GateLap GetChild(this GateLap lap, int index) { return (GateLap)lap["Entries"].DataList[index].DataDictionary; }

    public static int EntryCount(this GateLap lap) { return lap["Entries"].DataList.Count; }

    public static DataToken GetEntry(this GateLap lap, int index) { return lap["Entries"].DataList[index]; }
    
    public static void AddEntry(this GateLap lap, DataToken gate) { lap["Entries"].DataList.Add(gate); }

    public static void RemoveEntry(this GateLap lap, DataToken gate) { lap["Entries"].DataList.Remove(gate); }
    
}
