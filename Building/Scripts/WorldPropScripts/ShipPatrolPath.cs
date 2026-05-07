
using System;
using UdonSharp;
using UdonToolkit;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;
using Random = UnityEngine.Random;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
[CustomEditor(typeof(ShipPatrolPath))]
public class ShipPatrolPathEditor : UTEditor
{
    private void OnSceneGUI()
    {
        Vector3[] path = ((ShipPatrolPath)target).path;
        Transform transform = ((ShipPatrolPath)target).transform;

        for (int i = 0; i < path.Length; i++)
        {
            Vector3 currentWorldPos = transform.TransformPoint(path[i]);
            path[i] = transform.InverseTransformPoint(Handles.PositionHandle(currentWorldPos, transform.rotation));
        }
    }
}
#endif
public class ShipPatrolPath : WorldPropTemplate
{
    public string[] ships;
    public Vector3[] path;
    private ActorPool _actorPool;

    
    public override DataDictionary SerializeProp()
    {
        DataList pathParameter = new DataList();
        for (int i = 0; i < path.Length; i++)
        {
            pathParameter.Add(path[i].ToDataToken());
        }

        DataList shipsParameter = new DataList();
        for (int i = 0; i < ships.Length; i++)
        {
            shipsParameter.Add(ships[i]);
        }

        DataDictionary parameters = new DataDictionary();
        parameters["Path"] = pathParameter;
        parameters["Ships"] = shipsParameter;
        return parameters;
    }

    public override void DeserializeProp(DataDictionary parameters)
    {
        DataList pathParameters = parameters["Path"].DataList;
        path = new Vector3[pathParameters.Count];
        for (int i = 0; i < pathParameters.Count; i++)
        {
            path[i] = pathParameters[i].ToVector3();
        }

        DataList shipsParameter = parameters["Ships"].DataList;
        ships = new string[shipsParameter.Count];
        for (int i = 0; i < shipsParameter.Count; i++)
        {
            ships[i] = shipsParameter[i].String;
        }

        DataDictionary shipParameters = new DataDictionary();
        DataList shipPath = new DataList();
        for (int i = 0; i < path.Length; i++)
        {
            shipPath.Add(transform.parent.InverseTransformPoint(transform.TransformPoint(path[i])).ToDataToken());
        }
        shipParameters["Path"] = shipPath;
        if (!Utilities.IsValid(_actorPool)) _actorPool = ActorPoolFinder.FindActorPool();
        for (int i = 0; i < ships.Length; i++)
        {
            _actorPool.SpawnActor(ships[i], Random.insideUnitSphere + transform.localPosition, transform.localRotation, shipParameters);
        }
    }
    
#if UNITY_EDITOR && !COMPILER_UDONSHARP
    private void OnDrawGizmos()
    {
        for (int i = 0; i < path.Length; i++)
        {
            Vector3 currentWorldPos = transform.TransformPoint(path[i]);
            Vector3 nextWorldPos = transform.TransformPoint(path[(i + 1) % path.Length]);
            DrawArrows(currentWorldPos, nextWorldPos);
        }
    }

    private void DrawArrows(Vector3 start, Vector3 end)
    {
        Gizmos.color = Color.black;
        Gizmos.DrawLine(start, end);
        Quaternion lookRotation = Quaternion.LookRotation(end - start);
        float distance = Vector3.Distance(start, end);
        float i = 0;
        while (i < distance)
        {
            i += 200;
            if (i > distance) break;
            float lerp = Mathf.InverseLerp(0, distance, i);
            Vector3 arrowPoint = Vector3.Lerp(start, end, lerp);
            Vector3 leftArrow = new Vector3(-50, 0, -100);
            leftArrow = lookRotation * leftArrow;
            leftArrow += arrowPoint;
            Gizmos.DrawLine(leftArrow, arrowPoint);
            Vector3 rightArrow = new Vector3(50, 0, -100);
            rightArrow = lookRotation * rightArrow;
            rightArrow += arrowPoint;
            Gizmos.DrawLine(rightArrow, arrowPoint);
        }
    }
    #endif
}
