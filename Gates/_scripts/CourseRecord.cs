
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

public class CourseRecord : DataDictionary
{
    public static CourseRecord Create(string hash, LapRecord lapRecord)
    {
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        CourseRecord course = new CourseRecord();
#else
        CourseRecord course = (CourseRecord)new DataDictionary();
#endif
        course["Hash"] = hash;
        course["BestLap"] = lapRecord;
        DataList laps = new DataList();
        laps.Add(lapRecord);
        course["Laps"] = laps;
        course["TotalTime"] = lapRecord.GetTime();
        course["TotalLaps"] = 1;
        return course;
    }
}

public static class CourseRecordExtensions
{

    public static string GetHash(this CourseRecord courseRecord)
    {
        return courseRecord["Hash"].String;
    }
    public static void SubmitTime(this CourseRecord courseRecord, LapRecord lapRecord)
    {
        courseRecord.AddTotalLaps(1);
        courseRecord.AddTotalTime(lapRecord.GetTime());
        if (lapRecord.GetTime() < courseRecord.GetBestTime())
        {
            courseRecord.SetBestLap(lapRecord);
        }
        EventTracker.Instance().TrackEvent(nameof(CourseRecord), nameof(SubmitTime), null)
            .AddParameter("CourseRecord", courseRecord.DeepClone())
            .AddParameter("LapRecord", lapRecord.DeepClone());

    }
    public static void SetBestLap(this CourseRecord courseRecord, LapRecord lapRecord)
    {
        courseRecord["BestLap"] = lapRecord;
    }
    public static LapRecord GetBestLap(this CourseRecord courseRecord)
    {
        return (LapRecord)courseRecord["BestLap"].DataDictionary;
    }
    
    public static double GetBestTime(this CourseRecord courseRecord)
    {
        return courseRecord.GetBestLap().GetTime();
    }

    public static void AddTotalLaps(this CourseRecord courseRecord, int count)
    {
        courseRecord["TotalLaps"] = courseRecord.GetTotalLaps() + count;
    }
    public static int GetTotalLaps(this CourseRecord courseRecord)
    {
        return Mathf.RoundToInt((float)courseRecord["TotalLaps"].Number);
    }

    public static double GetTotalTime(this CourseRecord courseRecord)
    {
        return courseRecord["TotalTime"].Number;
    }

    public static void AddTotalTime(this CourseRecord courseRecord, double time)
    {
        courseRecord["TotalTime"] = courseRecord.GetTotalTime() + time;
    }
}