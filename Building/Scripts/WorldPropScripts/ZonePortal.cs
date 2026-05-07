
using System;
using UdonSharp;
using UdonToolkit;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
[CustomEditor(typeof(ZonePortal))]
public class ZonePortalEditor : UTEditor { }
#endif
public class ZonePortal : WorldPropTemplate
{
    public Transform entryPoint;
    public string thisPortal;
    public string targetZone;
    public string targetPortal;
    private DataDictionary _currentParameters;

    public override void DeserializeProp(DataDictionary parameters)
    {
        if (!Utilities.IsValid(parameters)) return;
        currentParameters = parameters.DeepClone();
        if (parameters.TryGetValue("targetZone", out DataToken targetZoneToken)) targetZone = targetZoneToken.String;
        if (parameters.TryGetValue("targetPortal", out DataToken targetPortalToken)) targetPortal = targetPortalToken.String;
        if (parameters.TryGetValue("thisPortal", out DataToken thisPortalToken)) thisPortal = thisPortalToken.String;
    }

    public override DataDictionary SerializeProp()
    {
        if (!Utilities.IsValid(currentParameters)) currentParameters = new DataDictionary();
        currentParameters["targetZone"] = targetZone;
        currentParameters["targetPortal"] = targetPortal;
        currentParameters["thisPortal"] = thisPortal;
        return currentParameters;
    }

    public void _OnShipTriggerEnter()
    {
        ZoneLoading zoneLoading = ZoneLoadingFinder.FindZoneLoading();
        zoneLoading.LoadWorldData(targetZone, targetPortal);
    }

    private ZoneLoading _zoneLoading;
    private float gizmoLength = 200;
    private float gizmoWidth = 100;
    private float gizmoBack = 100;

    #if UNITY_EDITOR && !COMPILER_UDONSHARP
    private void OnDrawGizmos()
    {
        if (!Utilities.IsValid(_zoneLoading)) _zoneLoading = ZoneLoadingFinder.FindZoneLoading();
        Handles.color = Color.black;
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.black;
        Handles.Label(transform.position, $" {_zoneLoading.GetCurrentZoneName()} {thisPortal} => {targetZone} {targetPortal}", style);
        
        Gizmos.color = Color.black;
        Gizmos.DrawLine(transform.TransformPoint(new Vector3(gizmoWidth,0,gizmoBack)), transform.TransformPoint(new Vector3(-gizmoWidth, 0, gizmoBack)));
        Gizmos.DrawLine(transform.TransformPoint(new Vector3(-gizmoWidth,0,gizmoBack)), transform.TransformPoint(new Vector3(-gizmoWidth, 0, -gizmoLength)));
        Gizmos.DrawLine(transform.TransformPoint(new Vector3(-gizmoWidth,0,-gizmoLength)), transform.TransformPoint(new Vector3(gizmoWidth, 0, -gizmoLength)));
        Gizmos.DrawLine(transform.TransformPoint(new Vector3(gizmoWidth,0,-gizmoLength)), transform.TransformPoint(new Vector3(gizmoWidth, 0, gizmoBack)));
    }

    
        
    [Button("Save zone")]
    public void SaveZone()
    {
        ZoneLoading zoneLoading = ZoneLoadingFinder.FindZoneLoading();
        zoneLoading.SaveZone();
    }
    
    [Button("Travel to")]
    public void EditorTravelTo()
    {
        Vector3 offset = SceneView.lastActiveSceneView.pivot - transform.position;
        ZoneLoading zoneLoading = ZoneLoadingFinder.FindZoneLoading();
        
        string tempTargetPortalName = targetPortal; //Grab the targetportal before loading zone so that we have it
        
        if (!zoneLoading.LoadZoneEditor(targetZone)) return;
        
        ZonePortal targetPortalObject = zoneLoading.GetPortal(tempTargetPortalName);
        if (targetPortalObject == null) return;
        
        Selection.objects = new Object[] { targetPortalObject.gameObject };
        SceneView.lastActiveSceneView.pivot = targetPortalObject.transform.position + offset;
    }

    [Button("Link new portal")]
    public void LinkNewPortal()
    {
        ZoneLoading zoneLoading = ZoneLoadingFinder.FindZoneLoading();
        zoneLoading.LinkNewPortal(this, targetZone, targetPortal);
    }
    
    #endif
}