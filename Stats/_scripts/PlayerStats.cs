
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDK3.Persistence;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

public class PlayerStats : UdonSharpBehaviour
{
    public StatsViewer statsViewer;
    [UdonSynced]
    private string _serializedData;
    private DataDictionary _data;
    private bool _restored;

    public CourseRecord GetCourseRecord(string hash)
    {
        if (_data.ContainsKey(hash)) return (CourseRecord)_data[hash].DataDictionary;
        return null;
    }
    public override void OnPlayerRestored(VRCPlayerApi player)
    {
        if (!player.IsOwner(gameObject)) return;
        _restored = true;
        if (_data == null) _data = new DataDictionary();
    }

    public override void OnPreSerialization()
    {
        if (VRCJson.TrySerializeToJson(_data, JsonExportType.Minify, out DataToken serialized) && serialized.TokenType == TokenType.String)
        {
            _serializedData = serialized.String;
        }
        ApplySerialization();
    }

    public override void OnDeserialization(DeserializationResult result)
    {
        if (VRCJson.TryDeserializeFromJson(_serializedData, out DataToken data) && data.TokenType == TokenType.DataDictionary)
        {
            _data = data.DataDictionary;
        }
        ApplySerialization();
    }

    private void ApplySerialization()
    {
        if (!Utilities.IsValid(_data)) return;
        DataList keys = _data.GetKeys();
        for (int i = 0; i < keys.Count; i++)
        {
            statsViewer.SetData(Networking.GetOwner(gameObject).displayName, GetCourseRecord(keys[i].String).GetBestTime());
        }
    }

    public void SubmitLap(string hash, LapRecord lapRecord)
    {
        if (!Networking.IsOwner(gameObject)) return;
        if (!_restored) return;
        double time = lapRecord.GetTime();
        if (!_data.ContainsKey(hash))
        {
            _data[hash] = CourseRecord.Create(hash, lapRecord);
        }
        else
        {
            CourseRecord courseRecord = (CourseRecord)_data[hash].DataDictionary;
            courseRecord.SubmitTime(lapRecord);
        }
        if (Utilities.IsValid(UdonShellReferenceManager.Instance())) UdonShellReferenceManager.Instance().udonShellCore.SendCommand((7453 << 2) * 5, $"message @a {time:N3}", false, false, false, false, false);
        RequestSerialization();
    }
    
    public static PlayerStats Find(VRCPlayerApi player)
    {
        var objects = Networking.GetPlayerObjects(player);
        for (int i = 0; i < objects.Length; i++)
        {
            if (!Utilities.IsValid(objects[i])) continue;
            PlayerStats foundScript = objects[i].GetComponentInChildren<PlayerStats>();
            if (Utilities.IsValid(foundScript)) return foundScript;
        }
        return null;
    }

    public LapRecord GetBestLap(string hash)
    {
        if (!_data.TryGetValue(hash, out DataToken trackRecord)) return null;
        return (LapRecord)trackRecord.DataDictionary["BestLap"].DataDictionary;
    }
}
