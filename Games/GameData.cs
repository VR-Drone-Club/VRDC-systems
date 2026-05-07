
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class GameData : UdonSharpBehaviour
{
    private DataDictionary _scores = new DataDictionary();
    private bool _pendingDeserialization;
    [UdonSynced]
    private string _dataJson;

    public static GameData Instance(VRCPlayerApi playerApi)
    {
        GameObject[] playerObjects = Networking.GetPlayerObjects(playerApi);
        for (int i = 0; i < playerObjects.Length; i++)
        {
            if (!Utilities.IsValid(playerObjects[i])) continue;
            GameData gameData = playerObjects[i].GetComponentInChildren<GameData>();
            if (!Utilities.IsValid(gameData)) continue;
            return gameData;
        }

        return null;
    }
    public DataDictionary GetScoreData(string gameName, string scoreName)
    {
        if (_pendingDeserialization) Deserialize();

        if (!_scores.ContainsKey(gameName)) _scores[gameName] = new DataDictionary();
        DataDictionary game = _scores[gameName].DataDictionary;
        if (!game.ContainsKey(scoreName)) game[scoreName] = new DataDictionary();
        return game[scoreName].DataDictionary;
    }   
    public void SetScore(string gameName, string scoreName, double value)
    {
        if (_pendingDeserialization) Deserialize();

        DataDictionary score = GetScoreData(gameName, scoreName);
        score["value"] = value;
        score["time"] = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
    }

    public bool HasScore(string gameName, string scoreName)
    {
        return GetScoreData(gameName, scoreName).ContainsKey("value");
    }
    public double GetScoreValue(string gameName, string scoreName)
    {
        if (_pendingDeserialization) Deserialize();

        DataDictionary score = GetScoreData(gameName, scoreName);
        return score["value"].Double;
    }

    public DateTime GetScoreTime(string gameName, string scoreName)
    {
        if (_pendingDeserialization) Deserialize();

        DataDictionary score = GetScoreData(gameName, scoreName);
        return new DateTime(1970, 1, 1) + TimeSpan.FromSeconds(score["time"].Double);
    }

    public override void OnPreSerialization()
    {
        if (!VRCJson.TrySerializeToJson(_scores, JsonExportType.Minify, out DataToken result))
        {
            Debug.LogError($"Failed to serialize score json: {result.ToString()}");
            return;
        }
        _dataJson = result.String;
    }

    public override void OnDeserialization(DeserializationResult result)
    {
        _pendingDeserialization = true;
    }

    private void Deserialize()
    {
        if (VRCJson.TryDeserializeFromJson(_dataJson, out DataToken result))
        {
            Debug.LogError($"Failed to deserialize score json: {result.ToString()}");
            return;
        }
        _scores = result.DataDictionary;

        _pendingDeserialization = false;
    }
}
