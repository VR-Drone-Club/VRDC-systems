
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDK3.Video.Components.Base;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class EventSync : UdonSharpBehaviour
{
    public MountedTurret mountedTurret;
    [UdonSynced] private byte[] _syncedEvents = new byte[50];
    [UdonSynced] private float[] _syncedTimestamps = new float[50];
    [UdonSynced] private float[] _syncedDependencyTimestamps = new float[50];
    [UdonSynced] private byte[] _instantEventSequence = new byte[0];
    
    private byte[] _localEvents = new byte[50];
    private float[] _localTimestamps = new float[50];
    private float[] _dependencyTimestamps = new float[50];

    private int _eventBufferPosition;
    private float _lastSyncedEvent;
    private int _lastEventProcessed;
    private int _numMatchesFound;
    private float _eventSequenceThreshold = 0.01f;
    private int _numEventsSynced;

    public void SyncEvent(byte eventIndex)
    {
        if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
        _numEventsSynced++;
        //Commented out to disable event sequences
        //if (_numMatchesFound < 10 && Time.realtimeSinceStartup - _eventSequenceThreshold > _lastSyncedEvent) RecognizeEventSequence();
        _syncedEvents[_eventBufferPosition] = eventIndex;
        _lastSyncedEvent = Time.realtimeSinceStartup;
        _localTimestamps[_eventBufferPosition] = _lastSyncedEvent;
        _dependencyTimestamps[_eventBufferPosition] = _lastSyncedEvent - 0.4f;
        
        //don't remember what this does
        //_dependencyTimestamps[_eventBufferPosition] = Networking.SimulationTime(Networking.GetOwner(_playerShip.gameObject));
        //if (_instantEventSequence.Length != 0) Debug.Log($"Recording event {eventIndex}, last event in sequence is {_instantEventSequence[_instantEventSequence.Length - 1]}");
        
        //Commented out to disable event sequences
        //if (_numMatchesFound > 5 && _instantEventSequence[_instantEventSequence.Length - 1] == eventIndex) MatchEventSequence();
        
        _eventBufferPosition = (_eventBufferPosition + 1) % _syncedEvents.Length;
        _syncedEvents[_eventBufferPosition] = 0;
        RequestSerialization();
    }

    private void MatchEventSequence()
    {
        //Debug.Log("Checking for matching event sequence");
        int searchIndex = _eventBufferPosition;
        float searchStartTime = _localTimestamps[searchIndex];
        int eventsSaved = 0;
        for (int i = _instantEventSequence.Length - 1; i >= 0; i--)
        {
            if (_instantEventSequence[i] != _syncedEvents[searchIndex]) return;
            if (Mathf.Abs(_localTimestamps[searchIndex] - searchStartTime) > _eventSequenceThreshold) return;
            eventsSaved++;
            searchIndex--;
            if (searchIndex < 0) searchIndex = _syncedTimestamps.Length - 1;
        }
        Debug.Log($"Found matching event sequence {_instantEventSequence.Length}, saved {eventsSaved} events");
        _numEventsSynced -= eventsSaved;
        _numEventsSynced++;
        _syncedEvents[searchIndex] = 128;
        _eventBufferPosition = searchIndex;
    }
    private void RecognizeEventSequence()
    {
        int searchIndex = _eventBufferPosition - 1;
        if (searchIndex < 0) searchIndex = _syncedTimestamps.Length - 1;
        
        float searchStartTime = _localTimestamps[searchIndex];
        int foundEvents = 0;
        string output = string.Empty;
        //Debug.Log($"Checking if {_localEvents[searchIndex]} {_localTimestamps[searchIndex]} is valid to include in sequence");
        while (Mathf.Abs(_localTimestamps[searchIndex] - searchStartTime) < _eventSequenceThreshold)
        {
            output += $"{_syncedEvents[searchIndex]} ";
            byte foundEvent = _syncedEvents[searchIndex];
            if (foundEvent == 0) break;
            if (foundEvent > 127) break;
            foundEvents++;
            
            searchIndex--;
            if (searchIndex < 0) searchIndex = _syncedTimestamps.Length - 1;
            //Debug.Log($"Checking if {_syncedEvents[searchIndex]} {_localTimestamps[searchIndex]} is valid to include in sequence");
        }
        Debug.Log($"Looking for event sequence, found {foundEvents} events: {output}");

        if (foundEvents > _instantEventSequence.Length)
        {
            _numMatchesFound = 0;
            _instantEventSequence = new byte[foundEvents];
            searchIndex = _eventBufferPosition - 1;
            if (searchIndex < 0) searchIndex = _syncedTimestamps.Length - 1;
            for (int i = _instantEventSequence.Length - 1; i >= 0; i--)
            {
                _instantEventSequence[i] = _syncedEvents[searchIndex];
                //_localEvents[searchIndex] = 0;
                searchIndex--;
                if (searchIndex < 0) searchIndex = _syncedTimestamps.Length - 1;
            }
            //_localEvents[searchIndex] = 128;
            _eventBufferPosition = (searchIndex + 1) % _syncedEvents.Length;
            Debug.Log($"New highest event sequence {foundEvents}: {output}");
        }
        else if (foundEvents == _instantEventSequence.Length)
        {
            _numMatchesFound++;
            Debug.Log($"Matched existing event sequence {_numMatchesFound} times");
        }
    }

    public override void OnPreSerialization()
    {
        Debug.Log($"Synced {_numEventsSynced} events this serialization");
        _numEventsSynced = 0;
        if (_lastSyncedEvent + 2 < Time.realtimeSinceStartup)
        {
            _syncedEvents = _localEvents;
            for (int i = _lastEventProcessed; _syncedEvents[i] != 0; i = (i + 1) % _syncedTimestamps.Length)
            {
                _syncedTimestamps[i] = _localTimestamps[i] - Time.realtimeSinceStartup;
                _syncedDependencyTimestamps[i] = _dependencyTimestamps[i] - Time.realtimeSinceStartup;
                _lastEventProcessed = i;
                //Debug.Log($"Buffer {i}: {_syncedTimestamps[i]} {GunEventStrings[_syncedEvents[i]]}");
            }
        }
    }

    public override void OnDeserialization(DeserializationResult result)
    {
        float time = result.sendTime;
        for (int i = _lastEventProcessed; _syncedEvents[i] != 0; i = (i + 1) % _syncedTimestamps.Length)
        {
            _localTimestamps[i] = _syncedTimestamps[i] + time;
            _dependencyTimestamps[i] = _syncedDependencyTimestamps[i] + time;
            _lastEventProcessed = i;
        }
        PlaybackEvents();
    }
    
    public void PlaybackEvents()
    {
        int count = 0;
        while (_localTimestamps[_eventBufferPosition] < Networking.SimulationTime(Networking.GetOwner(gameObject)) && _syncedEvents[_eventBufferPosition] != 0)
        {
            //Debug.Log($"Playing back event buffer at {_eventBufferPosition}");
            count++;
            byte syncedEvent = _syncedEvents[_eventBufferPosition];
            if (syncedEvent == 0)
            {
                
            }
            if (syncedEvent == 128)
            {
                for (int i = 0; i < _instantEventSequence.Length; i++)
                {
                    mountedTurret.RunEvent((GunEvent)_instantEventSequence[i], _localTimestamps[_eventBufferPosition] - Time.realtimeSinceStartup, _dependencyTimestamps[_eventBufferPosition]);
                }
            }
            else
            {
                mountedTurret.RunEvent((GunEvent)_syncedEvents[_eventBufferPosition], _localTimestamps[_eventBufferPosition] - Time.realtimeSinceStartup, _dependencyTimestamps[_eventBufferPosition]);
            }
            _eventBufferPosition = (_eventBufferPosition + 1) % _localTimestamps.Length;
            if (count > _syncedTimestamps.Length)
            {
                //Debug.Log("Exceeded number of events in playback");
                return;
            }
        }
        SendCustomEventDelayedSeconds(nameof(PlaybackEvents), _localTimestamps[_eventBufferPosition] - Time.realtimeSinceStartup);
    }
}
