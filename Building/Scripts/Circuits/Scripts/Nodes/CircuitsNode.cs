
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

public enum NodeType
{
    ExternNode,
    NativeNode,
}
public abstract class CircuitsNode : UdonSharpBehaviour
{
    internal CircuitsManager _circuitsManager;
    [NonSerialized] public string[] outputNames;
    [NonSerialized] public string[] inputNames;

    public Transform[] panelPositions;

    private bool _initialized;
    internal void Initialize()
    {
        GameObject circuitsManagerObject = GameObject.Find("CircuitsManager");
        _circuitsManager = circuitsManagerObject.GetComponent<CircuitsManager>();
        _initialized = true;
    }

    internal void SendProperties()
    {
        if (!_initialized) Initialize();
        _circuitsManager.SetNodeProperty(this, "NumOutputs",  outputNames.Length);
        _circuitsManager.SetNodeProperty(this, "NumInputs",  inputNames.Length);
    }
    public Transform GetNearestPanelPosition()
    {
        Quaternion headRotation = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;

        Transform nearestPosition = null;
        float nearestAngle = float.MaxValue;
        for (int i = 0; i < panelPositions.Length; i++)
        {
            float angle = Quaternion.Angle(headRotation, panelPositions[i].rotation);
            if (angle < nearestAngle)
            {
                nearestPosition = panelPositions[i];
                nearestAngle = angle;
            }
        }

        return nearestPosition;
    }

    
    public abstract Vector3 GetNearestWirePosition(int index, bool isInput, Vector3 other);

    public abstract NodeType GetNodeType();

    public abstract bool RequireAllInputs();
}