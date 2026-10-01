
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

public class MissionRecord : DataDictionary
{
    public static MissionRecord Create(string uuid, float time)
    {
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        MissionRecord missionRecord = new MissionRecord(); // in regular C# we just create the class directly
#else
        MissionRecord missionRecord = (MissionRecord)new DataDictionary(); // In U# we create a DataDictionary and cast it to the class
#endif
        
        return missionRecord;
    }
}
