
using System;
using System.IO;
using UdonSharp;
using UdonToolkit;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDK3.Video.Components;
using VRC.SDK3.Video.Components.Base;
using VRC.SDKBase;
using VRC.Udon;

#if UNITY_EDITOR && !COMPILER_UDONSHARP

[CustomEditor(typeof(GunSlider))]
public class GunSliderEditor : UTEditor
{
    protected virtual void OnSceneGUI()
    {
        GunSlider sender = (GunSlider)target;
        Transform parent = sender.transform.parent;
        for (int i = 0; i < sender.path.Length; i++)
        {
            sender.path[i] = parent.InverseTransformPoint(Handles.PositionHandle(parent.TransformPoint(sender.path[i]), parent.rotation));
            if (i + 1 < sender.path.Length)
            {
                Handles.DrawLine(parent.TransformPoint(sender.path[i]), parent.TransformPoint(sender.path[i+1]));
            }
        }
    }
}
#endif
public enum InteractEventRequirements
{
    None,
    Held,
    NotHeld,
}
public class GunSlider : UdonSharpBehaviour
{
    public UdonBehaviour target;
    [ListView("Path")]
    public Vector3[] path;
    [ListView("Path")]
    public float[] pull;

    [ListView("Events")]
    public float[] triggerPoints;
    [ListView("Events")]
    public bool[] directions;
    [ListView("Events")]
    public string[] events;
    [ListView("Events")]
    public InteractEventRequirements[] requirements;
    
    private bool _held;

    private int _position;
    private int _lastEvent;
    private float _evaluation;

    [RecursiveMethod]
    private void SetEvaluation(float value)
    {
        for (int i = 0; i < events.Length; i++)
        {
            if (directions[i] && _lastEvent != i && _evaluation < triggerPoints[i] && value >= triggerPoints[i]) //If going up
            {
                if (!CheckRequirements(requirements[i], i)) continue;
                //Debug.Log($"Passed threshold {triggerPoints[i]} going up, triggering {events[i]}");
                _lastEvent = i;
                target.SendCustomEvent(events[i]);
            }
            if (!directions[i] && _lastEvent != i && _evaluation > triggerPoints[i] && value <= triggerPoints[i]) //If going down
            {
                if (!CheckRequirements(requirements[i], i)) continue;
                //Debug.Log($"Passed threshold {triggerPoints[i]} going down, triggering {events[i]}");
                _lastEvent = i;
                target.SendCustomEvent(events[i]);
            }
        }
        _evaluation = value;
        _position = Mathf.Clamp(Mathf.FloorToInt(_evaluation), 0, path.Length - 2);
        ApplyPosition();
    }

    private bool CheckRequirements(InteractEventRequirements requirements, int index)
    {
        if (_lastEvent == index) return false;
        //Debug.Log($"Checking requirement {requirements}, index {index}, held {_held}");
        switch (requirements)
        {
            case InteractEventRequirements.None: return true;
            case InteractEventRequirements.Held: return _held;
            case InteractEventRequirements.NotHeld: return !_held;
        }

        return true;
    }
    private Transform _parent;
    void Start()
    {
        _parent = transform.parent;
    }

    public override void PostLateUpdate()
    {
        Step();
    }

    public override void OnPickup()
    {
        _held = true;
    }

    public override void OnDrop()
    {
        _held = false;
    }

    private void Step()
    {
        float evaluation = Evaluate();
        if (!_held) evaluation = ProcessPull(evaluation);
        SetEvaluation(_position + evaluation);
    }

    private void ApplyPosition()
    {
        _position = Mathf.Clamp(Mathf.FloorToInt(_evaluation), 0, path.Length - 2);
        transform.localPosition = Vector3.Lerp(path[_position], path[_position + 1], _evaluation - _position);
        transform.localRotation = Quaternion.identity;
    }

    private float ProcessPull(float evaluation)
    {
        if (!Networking.IsOwner(gameObject)) return 0;
        _position = Mathf.Clamp(Mathf.FloorToInt(_evaluation), 0, path.Length - 2);
        return evaluation + (pull[_position + 1] - pull[_position]) * Time.deltaTime;
    }

    private float Evaluate()
    {
        _position = Mathf.Clamp(Mathf.FloorToInt(_evaluation), 0, path.Length - 2);
        Vector3 previousPosition = path[_position];
        Vector3 nextPosition = path[_position + 1];
        Quaternion prevToNext = Quaternion.LookRotation(nextPosition - previousPosition);
        return InverseTransformPoint(previousPosition, prevToNext, new Vector3(1, 1, Vector3.Distance(previousPosition, nextPosition)), transform.localPosition).z;
    }

    public void JumpTo(string eventName)
    {
        for (int i = 0; i < events.Length; i++)
        {
            if (events[i] != eventName) continue;
            //Debug.Log($"{name} jumping to {eventName} position {triggerPoints[i]}");
            SetEvaluation(triggerPoints[i]);
            target.SendCustomEvent(events[i]);
            _lastEvent = i;
            return;
        }
    }
    
    public Vector3 TransformPoint(Vector3 originPos, Quaternion originRot, Vector3 point)
    {
        point = originRot * point;
        point += originPos;
        return point;
    }

    public Quaternion TransformRotation(Quaternion originRot, Quaternion rotation)
    {
        return originRot * rotation;
    }
    public Vector3 InverseTransformPoint(Vector3 originPos, Quaternion originRot, Vector3 scale, Vector3 point)
    {
        point -= originPos;
        point = Quaternion.Inverse(originRot) * point;
        point.x /= scale.x;
        point.y /= scale.y;
        point.z /= scale.z;
        return point;
    }
    public Quaternion InverseTransformRotation(Quaternion originRot, Quaternion rotation)
    {
        return Quaternion.Inverse(originRot) * rotation;
    }

    #if UNITY_EDITOR && !COMPILER_UDONSHARP
    private void OnDrawGizmos()
    {
        if (Application.isPlaying) return;
        if (path == null || path.Length == 0) return;
        _parent = transform.parent;
        Step();
    }
    #endif
}
