
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public abstract class AbstractLockstepObject : WorldPropTemplate
{
        [UdonSynced]
        public Vector3 syncedPosition;
        [UdonSynced]
        public Quaternion syncedRotation;

        private float[] _timestampBuffer = new float[10];
        private Vector3[] _positionBuffer = new Vector3[10];
        private Quaternion[] _rotationBuffer = new Quaternion[10];
        private int _playbackBufferIndex;
        private int _recordingBufferIndex;

        private float _sleepTimer = 5;
        private float _sleepTime = 1f;
        private bool _sleeping;

        private float _nextSyncTime;
        private float _minSyncInterval = 0.2f;
        private float _maxSyncInterval = 3f;
        private float _minSyncDistance = 100f;
        private float _maxSyncDistance = 1000f;
        private float _lastEvaluationTime;
        private float _evaluationInterval = 0.2f;

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
                if (_sleeping == value) return;
                _sleeping = value;
                if (!_sleeping)
                {
                    _sleepTimer = Time.timeSinceLevelLoad;
                }
            }
            get
            {
                if (_sleeping) return true;
                if (_sleepTimer + _sleepTime < Time.timeSinceLevelLoad)
                {
                    _sleeping = true;
                    return true;
                }
                return false;
            }
        }

        internal virtual void Start()
        {
            syncedPosition = transform.localPosition;
            syncedRotation = transform.localRotation;
            _lastEvaluationTime = Time.realtimeSinceStartup;
        }

        internal virtual void Update()
        {
            if (Networking.IsOwner(gameObject))
            {
                if (_lastEvaluationTime + _evaluationInterval < Time.realtimeSinceStartup)
                {
                    _lastEvaluationTime = Time.realtimeSinceStartup;
                    Evaluate(_evaluationInterval);
                }
                
                if (_nextSyncTime < Time.realtimeSinceStartup)
                {
                    _nextSyncTime = Time.realtimeSinceStartup + Mathf.Lerp(_minSyncInterval, _maxSyncInterval, Mathf.InverseLerp(_minSyncDistance, _maxSyncDistance, Vector3.Distance(Networking.LocalPlayer.GetPosition(), transform.position)));
                    RequestSerialization();
#if UNITY_EDITOR
                    OnPreSerialization();
#endif
                }
            }
            
            if (Sleeping) return;
            ApplySyncedPosition();
        }

        public virtual void GetPositionAndRotation(out Vector3 position, out Quaternion rotation)
        {
            position = transform.localPosition;
            rotation = transform.localRotation;
        }

        internal virtual void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
            transform.localPosition = position;
            transform.localRotation = rotation;
        }

        public bool GetPositionAndRotationAtTime(float time, out Vector3 position, out Quaternion rotation)
        {
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
                    return true;
                }
            }

            //Debug.Log($"Suitable timestamp could not be found for {time} ({time - Time.realtimeSinceStartup} delta), falling back to current position and rotation. Highest found was {highest} and lowest {lowest}");
            GetPositionAndRotation(out position, out rotation); //If a suitable timestamp can't be found, fall back to default position and rotation
            return false;
        }
        public bool GetPosRotVelAtTime(float time, out Vector3 position, out Quaternion rotation, out Vector3 velocity)
        {
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
                    return true;
                }
            }

            //Debug.Log($"Suitable timestamp could not be found for {time} ({time - Time.realtimeSinceStartup} delta), falling back to current position and rotation. Highest found was {highest} and lowest {lowest}");
            GetPositionAndRotation(out position, out rotation); //If a suitable timestamp can't be found, fall back to default position and rotation
            velocity = Vector3.zero;
            return false;
        }

        internal abstract void Evaluate(float delta);
    
        public override void OnPreSerialization()
        {
            RecordToBuffer(_lastEvaluationTime);
        }

        public override void OnDeserialization(DeserializationResult result)
        {
            RecordToBuffer(result.sendTime);
        }

        private void RecordToBuffer(float time)
        { 
            //Debug.Log($"Recording to buffer {_recordingBufferIndex} {syncedPosition} at {time}");
            _timestampBuffer[_recordingBufferIndex] = time;
            _positionBuffer[_recordingBufferIndex] = syncedPosition;
            _rotationBuffer[_recordingBufferIndex] = syncedRotation;
            _recordingBufferIndex++;
            if (_recordingBufferIndex >= _timestampBuffer.Length)
            {
                _recordingBufferIndex = 0;
            }

            Sleeping = false;
        }


        internal void ApplySyncedPosition()
        {
            float targetTime = Time.realtimeSinceStartup - 1f;
            /*
            float targetTime = Networking.IsOwner(gameObject) 
                ? Time.realtimeSinceStartup - 0.4f
                : Networking.SimulationTime(Networking.GetOwner(gameObject));
            */
            GetPositionAndRotationAtTime(targetTime, out Vector3 position, out Quaternion rotation);
            SetPositionAndRotation(position, rotation);
        }
}
