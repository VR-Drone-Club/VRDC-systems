
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

public enum ScoreType
{
    Points,
    Time,
}

public enum ScoreDirection
{
    Ascending,
    Descending
}
public class GameManager : UdonSharpBehaviour
{
    private DataDictionary _data = new DataDictionary();
    private DataDictionary _gameData = new DataDictionary();

    public static GameManager Instance()
    {
        GameObject gameManagerObject = GameObject.Find(nameof(GameManager));
        if (!Utilities.IsValid(gameManagerObject)) return null;
        return gameManagerObject.GetComponent<GameManager>();
    }
    void Start()
    {
        
    }

    public void RegisterGame(string gameName)
    {
        DataDictionary gameData = new DataDictionary();
        _data[gameName] = gameData;
        gameData["name"] = gameName;
        gameData["scores"] = new DataList();
    }

    public DataList GetGames()
    {
        return _data.GetKeys();
    }
    public DataDictionary GetGame(string gameName)
    {
        return _data[gameName].DataDictionary;
    }

    public DataList GetScores(string gameName)
    {
        return GetGame(gameName)["scores"].DataList;
    }

    public DataDictionary GetScoreData(string gameName, string scoreName)
    {
        DataList list = GetGame(gameName)["scores"].DataList;
        for (int i = 0; i < list.Count; i++)
        {
            DataDictionary score = list[i].DataDictionary;
            if (score["name"] == scoreName) return score;
        }
        return null;
    }
    public ScoreType GetScoreType(string gameName, string scoreName)
    {
        DataDictionary score = GetScoreData(gameName, scoreName);
        return (ScoreType)(int)score["type"].Double;
    }
    public ScoreDirection GetScoreDirection(string gameName, string scoreName)
    {
        DataDictionary score = GetScoreData(gameName, scoreName);
        return (ScoreDirection)(int)score["direction"].Double;
    }

    public void RegisterScore(string gameName, string scoreName, ScoreType scoreType, ScoreDirection scoreDirection)
    {
        DataDictionary score = new DataDictionary();
        GetScores(gameName).Add(score);
        score["name"] = scoreName;
        score["type"] = new DataToken((int)scoreType);
        score["direction"] = new DataToken((int)scoreDirection);
    }

    public void SetScore(string gameName, string scoreName, double value)
    {
        GameData gameData = GameData.Instance(Networking.LocalPlayer);
        gameData.SetScore(gameName, scoreName, value);
    }

    public GameData GetGameData(VRCPlayerApi player)
    {
        if (!_gameData.ContainsKey(player.playerId))
        {
            _gameData[player.playerId] = GameData.Instance(player);
            return (GameData)_gameData[player.playerId].Reference;
        }
        return (GameData)_gameData[player.playerId].Reference;
    }

    public double GetScoreValue(VRCPlayerApi player, string gameName, string scoreName)
    {
        GameData gameData = GetGameData(player);
        return gameData.GetScoreValue(gameName, scoreName);
    }
    public DateTime GetScoreTime(VRCPlayerApi player, string gameName, string scoreName)
    {
        GameData gameData = GameData.Instance(player);
        return gameData.GetScoreTime(gameName, scoreName);
    }

}
