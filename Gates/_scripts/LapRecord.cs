using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDK3.Data;

public class LapRecord : DataDictionary
{
    public static LapRecord Create()
    {
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        LapRecord lap = new LapRecord();
        #else
        LapRecord lap = (LapRecord)new DataDictionary();
#endif
        lap["Splits"] = new DataList();
        return lap;
    }
}

public static class LapRecordExtensions
{
    public static void AddSplit(this LapRecord lapRecord, double time)
    {
        lapRecord["Splits"].DataList.Add(time);
    }

    public static double GetSplit(this LapRecord lapRecord, int index)
    {
        return lapRecord["Splits"].DataList[index].Double;
    }

    public static int GetSplitCount(this LapRecord lapRecord)
    {
        return lapRecord["Splits"].DataList.Count;
    }

    public static double GetTime(this LapRecord lapRecord)
    {
        DataList splits = lapRecord["Splits"].DataList;
        return splits[splits.Count - 1].Double;
    }
    
}