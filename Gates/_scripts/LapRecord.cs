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
        lap["Time"] = 0d;
        return lap;
    }
}

public static class LapRecordExtensions
{
    public static double GetTime(this LapRecord lapRecord)
    {
        return lapRecord["Time"].Double;
    }
    public static void SetTime(this LapRecord lapRecord, double time)
    {
        lapRecord["Time"] = time;
    }
    public static void AddSplit(this LapRecord lapRecord, double time)
    {
        lapRecord["Splits"].DataList.Add(time);
        if (time > lapRecord.GetTime()) lapRecord.SetTime(time);
    }

    public static double GetSplit(this LapRecord lapRecord, int index)
    {
        return lapRecord["Splits"].DataList[index].Double;
    }
    public static bool GetSplitType(this LapRecord lapRecord, int index)
    {
        return lapRecord["SplitType"].DataList[index].Boolean;
    }

    public static int GetSplitCount(this LapRecord lapRecord)
    {
        return lapRecord["Splits"].DataList.Count;
    }

    public static void SetCompleted(this LapRecord lapRecord)
    {
        lapRecord["Completed"] = true;
    }

    public static bool GetCompleted(this LapRecord lapRecord)
    {
        return lapRecord.ContainsKey("Completed") && lapRecord["Completed"] == true;
    }
}