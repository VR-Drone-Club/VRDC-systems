using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace WorldPropScripts
{
    public class AbstractSyncedObject : PropTemplate
    {
        [UdonSynced]
        private Vector3 _syncedPosition;
        [UdonSynced]
        private Quaternion _syncedRotation;

        private float[] _timestampBuffer = new float[10];
        private Vector3[] _positionBuffer = new Vector3[10];
        private Quaternion[] _rotationBuffer = new Quaternion[10];
        private int _playbackBufferIndex;
        private int _recordingBufferIndex;

        private float _sleepTimer = 5;
        private float _sleepTime = 1f;
        private bool _sleeping;

        private float _lastSyncTime;
        private float _syncInterval = 0.1f;
        private float _sufferingSyncInterval = 0.05f;

        private float _oldTimestamp = float.MinValue;
        private Vector3 _oldPosition;
        private Quaternion _oldRotatation;
        
        private float _newTimestamp = float.MinValue;
        private Vector3 _newPosition;
        private Quaternion _newRotation;
        
        internal bool Sleeping
        {
            set
            {
                if (_sleeping != value)
                {
                    Debug.Log($"{name} sleeping changed to {value}");
                }
                _sleeping = value;
                if (!_sleeping)
                {
                    _sleepTimer = Time.timeSinceLevelLoad + _sleepTime;
                }
            }
            get
            {
                if (_sleeping) return true;
                if (_sleepTimer < Time.timeSinceLevelLoad) Sleeping = true;
                return _sleeping;
            }
        }

        internal bool IsActive()
        {
            Vector3 position;
            Quaternion rotation;
            if (Networking.IsOwner(gameObject))
            {
                GetPositionAndRotation(out position, out  rotation);
            }
            else
            {
                position = _syncedPosition;
                rotation = _syncedRotation;
            }
            float distance = Vector3.Distance(_lastRecordedPosition, position);
            float angle = Quaternion.Angle(_lastRecordedRotation, rotation);
            return (distance > 0.5f || angle > 1f);
        }
        internal virtual void Update()
        {
            if (Networking.IsOwner(gameObject))
            {
                if (IsActive()) Sleeping = false;
                if (Sleeping) return;
                if (_lastSyncTime + (Networking.IsClogged ? _sufferingSyncInterval : _syncInterval) < Time.timeSinceLevelLoad)
                {
                    _lastSyncTime = Time.timeSinceLevelLoad;
                    RequestSerialization();
                    #if UNITY_EDITOR
                    OnPreSerialization();
                    #endif
                }
            }
            else
            {
                if (Sleeping) return;
                PreApplyPosition();
                ApplySyncedPosition();
                PostApplyPosition();
            }
        }

        public virtual void GetPositionAndRotation(out Vector3 position, out Quaternion rotation)
        {
            position = transform.localPosition;
            rotation = transform.localRotation;
        }

        public virtual Vector3 GetPosition()
        {
            return transform.localPosition;
        }
        internal virtual void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
            transform.localPosition = position;
            transform.localRotation = rotation;
        }

        public bool GetPositionAndRotationAtTime(float time, out Vector3 position, out Quaternion rotation, out Vector3 velocity)
        {
            if (Sleeping)
            {
                Debug.Log($"{name} is sleeping, should be safe to return raw position instead of time-corrected");
                GetPositionAndRotation(out position, out rotation);
                velocity = Vector3.zero;
                return true;
            }
            //Debug.Log($"Looking for position at time {time} which is {time - Time.realtimeSinceStartup} in the past");
            float lowest = float.MaxValue;
            float highest = float.MinValue;
            for (int i = 0; i < _timestampBuffer.Length; i++)
            {
                if (_timestampBuffer[i] < lowest) lowest = _timestampBuffer[i];
                if (_timestampBuffer[i] > highest) highest = _timestampBuffer[i];
                int j = (i + 1) % _timestampBuffer.Length;
                //Debug.Log($"Checking timestamp buffer between {i} {_timestampBuffer[i]} and {j} {_timestampBuffer[j]}");
                if (time >= _timestampBuffer[i] && time <= _timestampBuffer[j])
                {
                    float lerp = Mathf.InverseLerp(_timestampBuffer[i], _timestampBuffer[j], time);
                    position = Vector3.Lerp(_positionBuffer[i], _positionBuffer[j], lerp);
                    rotation = Quaternion.Slerp(_rotationBuffer[i], _rotationBuffer[j], lerp);
                    velocity = ((_positionBuffer[j] - _positionBuffer[i]) / (_timestampBuffer[j] - _timestampBuffer[i]));
                    //Debug.Log($"Getting velocity between {_positionBuffer[i]} {_timestampBuffer[j]} and {_positionBuffer[i]} {_timestampBuffer[j]}");
                    //Debug.Log($"Found timestamp buffer that was {lerp} between {_timestampBuffer[i]} and {_timestampBuffer[j]}, resulting in velocity {velocity} and position {position} which is {Vector3.Distance(position, GetPosition())} meters away from real time");
                    return true;
                }
            }

            //Debug.Log($"Suitable timestamp could not be found for {time} ({time - Time.realtimeSinceStartup} delta), falling back to current position and rotation. Highest found was {highest} and lowest {lowest}");
            GetPositionAndRotation(out position, out rotation); //If a suitable timestamp can't be found, fall back to default position and rotation
            velocity = Vector3.zero;
            return false;
        }
        
        public override void OnPreSerialization()
        {
            GetPositionAndRotation(out _syncedPosition, out _syncedRotation);
            RecordToBuffer(Time.realtimeSinceStartup);
        }

        public override void OnDeserialization(DeserializationResult result)
        {
            RecordToBuffer(result.sendTime);
        }

        private Vector3 _lastRecordedPosition;
        private Quaternion _lastRecordedRotation;
        private void RecordToBuffer(float time)
        {
            if (!IsActive())
            {
                return;
            }
            Sleeping = false;
            _lastRecordedPosition = _syncedPosition;
            _lastRecordedRotation = _syncedRotation;
            
            _timestampBuffer[_recordingBufferIndex] = time;
            _positionBuffer[_recordingBufferIndex] = _syncedPosition;
            _rotationBuffer[_recordingBufferIndex] = _syncedRotation;
            _recordingBufferIndex++;
            if (_recordingBufferIndex >= _timestampBuffer.Length)
            {
                _recordingBufferIndex = 0;
            }
        }


        private void PullFromBuffer()
        {
            _oldPosition = _newPosition;
            _oldRotatation = _newRotation;

            _oldTimestamp = _newTimestamp;

            _newTimestamp = _timestampBuffer[_playbackBufferIndex];
            _newPosition = _positionBuffer[_playbackBufferIndex];
            _newRotation = _rotationBuffer[_playbackBufferIndex];
            _playbackBufferIndex++;
            if (_playbackBufferIndex >= _timestampBuffer.Length)
            {
                _playbackBufferIndex = 0;
            }
            //Debug.Log($"{name} Pulling {_newPosition} from buffer position {_playbackBufferIndex}");
        }

        internal virtual void PreApplyPosition()
        {
            
        }
        internal virtual void PostApplyPosition()
        {
            
        }
        internal void ApplySyncedPosition()
        {
            float targetTime = Networking.SimulationTime(Networking.GetOwner(gameObject));
            //Debug.Log($"Simulating {Time.realtimeSinceStartup - targetTime} in the past");

            GetPositionAndRotationAtTime(targetTime, out Vector3 position, out Quaternion rotation, out Vector3 velocity);
            SetPositionAndRotation(position, rotation);
/*
            if (targetTime > _newTimestamp)
            {
                PullFromBuffer();
            }

            float lerpTime = Mathf.InverseLerp(_oldTimestamp, _newTimestamp, targetTime);

            SetPositionAndRotation(Vector3.Lerp(_oldPosition, _newPosition, lerpTime), Quaternion.Slerp(_oldRotatation, _newRotation, lerpTime));
            */
        }
    }
}