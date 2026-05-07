
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

public class PointCup : UdonSharpBehaviour
{

    [UdonSynced]
    private string[] _players;

    [UdonSynced]
    private int[] _totalPoints;

    [UdonSynced]
    private int[] _roundPoints;

    [UdonSynced]
    private bool[] _finalist;

    [UdonSynced]
    private int[] _winner;

    [UdonSynced]
    public int qualifyingThreshold = 500;

    [UdonSynced] 
    public int currentRound;

    public CupTrial[] rounds;
    public float[] timePerRound;

    private float _timeRoundStarted;
    private int _appliedRound = -1;
    
    void Start()
    {
        
    }

    public override void OnDeserialization(DeserializationResult result)
    {
        ApplyState();
    }

    public override void OnPreSerialization()
    {
        ApplyState();
    }

    public void ApplyState()
    {
        if (_appliedRound != currentRound)
        {
            if (_appliedRound != -1)
            {
                rounds[_appliedRound % rounds.Length].EndRound();
            }
            rounds[currentRound % rounds.Length].BeginRound();
            _appliedRound = currentRound;
        }
    }
    
    [NetworkCallable]
    public void RegisterPlayer(string playerName)
    {
        if (!Networking.IsOwner(gameObject)) return;
        if (_players.Contains(playerName)) return;
        _players = _players.Add(playerName);
        _totalPoints = _totalPoints.Add(0);
        _roundPoints = _roundPoints.Add(0);
        _finalist = _finalist.Add(false);
        _winner = _winner.Add(0);
        RequestSerialization();
    }

    [NetworkCallable]
    public void UnregisterPlayer(string playerName)
    {
        if (!Networking.IsOwner(gameObject)) return;
        if (!_players.Contains(playerName)) return;
        int index = _players.IndexOf(playerName);
        _players = _players.RemoveAt(index);
        _totalPoints = _totalPoints.RemoveAt(index);
        _roundPoints = _roundPoints.RemoveAt(index);
        _finalist = _finalist.RemoveAt(index);
        _winner = _winner.RemoveAt(index);
        RequestSerialization();
    }
    
    [NetworkCallable]
    public void SetRoundPoints(string playerName, int round, int points)
    {
        if (!Networking.IsOwner(gameObject)) return;
        if (round != currentRound) return;
        int index = _players.IndexOf(playerName);
        if (index == -1) return;
        _totalPoints[index] = points;
        RequestSerialization();
    }

    [NetworkCallable]
    public void SetWinner(string playerName)
    {
        Debug.Log($"{playerName} won!");
    }

    public void FinishRound()
    {
        if (!Networking.IsOwner(gameObject)) return;
        
        // transfer round points to total points
        for (int i = 0; i < _players.Length && i < _totalPoints.Length && i < _roundPoints.Length; i++)
        {
            _totalPoints[i] += _roundPoints[i];
            _roundPoints[i] = 0;
        }
        
        // switch to next round
        currentRound++;
        
        // find player with highest points
        int highestRoundPoints = 0;
        int highestRoundPlayer = -1;
        for (int i = 0; i < _players.Length; i++)
        {
            if (_roundPoints[i] > highestRoundPoints) highestRoundPoints = _roundPoints[i];
            highestRoundPlayer = i;
        }
        
        // check if that player is a finalist
        if (highestRoundPlayer != -1 && _finalist[highestRoundPlayer])
        {
            SetWinner(_players[highestRoundPlayer]);
        }
        
        // check for new finalists
        for (int i = 0; i < _players.Length && i < _totalPoints.Length; i++)
        {
            if (_totalPoints[i] >= qualifyingThreshold)
            {
                _finalist[i] = true;
            }
        }
        
        RequestSerialization();
    }
}