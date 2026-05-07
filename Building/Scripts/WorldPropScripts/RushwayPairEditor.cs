
#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UdonToolkit;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RushwayPair))]
public class RushwayPairEditor : UTEditor
{
    protected virtual void OnSceneGUI()
    {
        RushwayPair rushway = (RushwayPair)target;

        EditorGUI.BeginChangeCheck();
        Vector3 newTargetPosition = Handles.PositionHandle(rushway.transform.TransformPoint(rushway.targetPosition), rushway.transform.rotation);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(rushway, "Change Look At Target Position");
            newTargetPosition.y = rushway.transform.position.y;
            rushway.targetPosition = rushway.transform.InverseTransformPoint(newTargetPosition);
            rushway.ApplyTargetPosition();
        }
        
        /*
        EditorGUI.BeginChangeCheck();
        float newTargetLength = Handles.ScaleSlider(rushway.length, rushway.transform.position + (rushway.transform.rotation * Vector3.forward * HandleUtility.GetHandleSize(rushway.transform.position)),rushway.transform.rotation * Vector3.forward,  rushway.transform.rotation, HandleUtility.GetHandleSize(rushway.transform.position), 50f);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(rushway, "Change length");
            rushway.length = Mathf.RoundToInt(newTargetLength);
            rushway.ApplyLength();
        }*/
    }
}
#endif