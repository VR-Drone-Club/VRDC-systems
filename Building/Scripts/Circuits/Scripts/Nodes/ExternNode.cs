
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

public abstract class ExternNode : CircuitsNode
{

    public abstract void SetInput(int index, float value);

    public abstract float GetOutput(int index);

    public Transform[] inputPositions;
    public Transform[] outputPositions;

    public override Vector3 GetNearestWirePosition(int index, bool isInput, Vector3 other)
    {
        float nearestDistance = float.MaxValue;
        Transform nearestPosition = transform;
        Transform[] positions = isInput ? inputPositions : outputPositions;
        for (int i = 0; i < positions.Length; i++)
        {
            float distance = Vector3.Distance(positions[i].position, other);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestPosition = positions[i];
            }
        }
        Debug.DrawLine(nearestPosition.position, other);
        return nearestPosition.position;
    }
    public override NodeType GetNodeType()
    {
        return NodeType.ExternNode;
    }
}
