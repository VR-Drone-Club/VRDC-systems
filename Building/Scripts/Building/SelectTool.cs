
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;
using VRDC_systems.Building.Scripts.Building;

public class SelectTool : BuilderTool
{
    public Transform MoveGizmo;
    public OutlineUtility OutlineUtility;
    private GameObject[] selectedProps = new GameObject[0];
    public Observable SelectStartPoint => GetProperty(nameof(SelectStartPoint));
    public Observable SelectEndPoint => GetProperty(nameof(SelectEndPoint));
    public Observable SelectStartTime => GetProperty(nameof(SelectStartTime));
    public Observable PrimaryDown => GetProperty(nameof(PrimaryDown));

    public override void Initialize(Builder builder, BuildManager buildManager)
    {
        base.Initialize(builder, buildManager);
        
        CreateProperty(nameof(SelectStartPoint),Vector3.zero.ToDataToken());
        CreateProperty(nameof(SelectEndPoint), Vector3.zero.ToDataToken());
        CreateProperty(nameof(SelectStartTime),Observable.Create(0f));
        CreateProperty(nameof(PrimaryDown), false);
        Builder.AddToolObject("SelectTool", MoveGizmo.gameObject);
        SetupActions();
    }

    public Sprite deleteSprite;
    public Sprite copySprite;
    public Sprite pasteSprite;
    public Sprite cutSprite;
    private void SetupActions()
    {
        Actions = new DataList();
        Actions.Add(ToolAction.Create("Delete", deleteSprite, Observable.Create(false)).Subscribe(this, nameof(DeleteSelected)));
        Actions.Add(ToolAction.Create("Copy", copySprite, Observable.Create(false)).Subscribe(this, nameof(CopySelected)));
        Actions.Add(ToolAction.Create("Paste", pasteSprite, Observable.Create(false)).Subscribe(this, nameof(PasteSelected)));
        Actions.Add(ToolAction.Create("Cut", cutSprite, Observable.Create(false)).Subscribe(this, nameof(CopySelected)).Subscribe(this, nameof(DeleteSelected)));
    }
    public override void ToolUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Delete)) DeleteSelected();
        if (Input.GetKeyDown(KeyCode.C) && Input.GetKey(KeyCode.LeftControl)) CopySelected();
        if (Input.GetKeyDown(KeyCode.X) && Input.GetKey(KeyCode.LeftControl))
        {
            CopySelected();
            DeleteSelected();
        }
        if (Input.GetKeyDown(KeyCode.V) && Input.GetKey(KeyCode.LeftControl)) PasteSelected();
        HandleGizmo();
        if (PrimaryDown.GetBool() && !_gizmoHit)
        {
            SelectEndPoint.SetValue(Builder.HoverPoint().ToDataToken());
        }
    }

    public override void SetToolActive(bool active)
    {
        foreach (var prop in selectedProps)
        {
            if (active)
            {
                OutlineUtility.AddOutline(prop);
            }
            else
            {
                OutlineUtility.RemoveOutline(prop);
            }
        }
    }

    public override void PrimaryAction(bool down)
    {
        Debug.Log($"SelectTool PrimaryAction {down}");
        if (down)
        {
            var hoverPoint = Builder.HoverPoint().ToDataToken();
            SelectStartPoint.SetValue(hoverPoint);
            SelectEndPoint.SetValue(hoverPoint);
            SelectStartTime.SetValue(Time.timeSinceLevelLoad);
            PrimaryDown.SetValue(true);
        }
        else
        {
            PrimaryDown.SetValue(false);
            if (_gizmoHit) return;
            if (SelectStartTime.GetFloat() + 0.2f > Time.timeSinceLevelLoad) // quick click
            {
                Builder.Raycast(Builder.UIRay(), QueryTriggerInteraction.Collide, out var pos, out var normal, out var o);
                if (!Input.GetKey(KeyCode.LeftShift)) ClearSelection();
                TrySelectObject(o);
            }
            else // hold region
            {
                if (!Input.GetKey(KeyCode.LeftShift)) ClearSelection();

                var min = Builder.WorldToCanvasPoint(Builder.canvas.TransformPoint(SelectStartPoint.GetVector3()));
                var max = Builder.WorldToCanvasPoint(Builder.canvas.TransformPoint(SelectEndPoint.GetVector3()));
                if (min.x > max.x)
                {
                    float actualMin = max.x;
                    max.x = min.x;
                    min.x = actualMin;
                }
                if (min.y > max.y)
                {
                    float actualMin = max.y;
                    max.y = min.y;
                    min.y = actualMin;
                }
                DataDictionary propPools = BuildManager.PropPools;
                DataList keys = propPools.GetKeys();
                for (int i = 0; i < keys.Count; i++)
                {
                    DataList pool = propPools[keys[i]].DataList;
                    for (int j = 0; j < pool.Count; j++)
                    {
                        GameObject prop = (GameObject)pool[j].Reference;
                        if (!prop.activeInHierarchy) continue;
                        Vector3 pos = Builder.WorldToCanvasPoint(prop.transform.position);
                        if (pos.x > min.x && pos.x < max.x && pos.y > min.y && pos.y < max.y)
                        {
                            TrySelectObject(prop);
                        }
                    }
                }

                /* // dumb raycast method
                Vector3 localStart = SelectStartPoint.GetVector3();
                Vector3 localEnd = SelectEndPoint.GetVector3();
                Vector3 start = Builder.canvas.TransformPoint(localStart);
                Vector3 end = Builder.canvas.TransformPoint(localEnd);
                Vector3 cornerA = Builder.canvas.TransformPoint(new Vector3(localStart.x, localEnd.y, 0));
                Vector3 cornerB = Builder.canvas.TransformPoint(new Vector3(localEnd.x, localStart.y, 0));

                Vector3 cam = Builder.cameraPosition;
                for (float i = 0; i <= 1; i += 0.1f)
                {
                    var intermediateA = Vector3.Lerp(start, cornerA, i);
                    var intermediateB = Vector3.Lerp(cornerB, end, i);
                    for (float j = 0; j <= 1; j += 0.1f)
                    {
                        var final = Vector3.Lerp(intermediateA, intermediateB, j);
                        Debug.DrawRay(cam,  final - cam, Color.white, 5);
                        Builder.SphereCast(0.5f, cam, final - cam, out Vector3 pos, out Vector3 normal, out GameObject o);
                        TrySelectObject(o);
                    }
                }*/
            }
        }
    }

    private void TrySelectObject(GameObject gameObject)
    {
        if (!Utilities.IsValid(gameObject)) return;
        if (selectedProps.Contains(gameObject)) return;
        if (BuildManager.IsRegisteredProp(gameObject))
        {
            SelectProp(gameObject);
            return;
        }

        while (Utilities.IsValid(gameObject.transform.parent))
        {
            gameObject = gameObject.transform.parent.gameObject;
            if (BuildManager.IsRegisteredProp(gameObject))
            {
                SelectProp(gameObject);
                return;
            }
        }
    }

    public void ClearSelection()
    {
        for (int i = 0; i < selectedProps.Length; i++)
        {
            OutlineUtility.RemoveOutline(selectedProps[i]);
        }
        selectedProps = new GameObject[0];
    }

    public void DeleteSelected()
    {
        foreach (var prop in selectedProps)
        {
            BuildManager.SyncedReturnProp(prop);
        }
        ClearSelection();
    }

    private DataList _copyBuffer = new DataList();
    public void CopySelected()
    {
        _copyBuffer.Clear();
        foreach (var prop in selectedProps)
        {
            DataList entry = new DataList();
            entry.Add(prop.name);
            entry.Add(prop.transform.position.ToDataToken());
            entry.Add(prop.transform.rotation.ToDataToken());
            _copyBuffer.Add(entry);
        }
    }

    public void PasteSelected()
    {
        ClearSelection();
        for (int i = 0; i < _copyBuffer.Count; i++)
        {
            DataList entry = _copyBuffer[i].DataList;
            SelectProp(BuildManager.SpawnPropSynced(entry[0].String, entry[1].ToVector3(), entry[2].ToQuaternion()));
        }
    }

    private void SelectProp(GameObject gameObject)
    {
        if (selectedProps.Contains(gameObject)) return;
        selectedProps = selectedProps.Add(gameObject);
        OutlineUtility.AddOutline(gameObject);
    }


    public Transform YArrow;
    public Transform ZArrow;
    public Transform XArrow;
    public Transform YPlane;
    public Transform XPlane;
    public Transform ZPlane;
    private bool _gizmoHit;
    private Vector3 _moveAxis;
    private Vector3 _intersectionPoint;
    private Vector3 _selectionPoint;
    private bool _planeOrAxis;
    private void HandleGizmo()
    {
        if (PrimaryDown.GetBool())
        {
            ApplyGizmoAxis();
        }
        else
        {
            FindGizmoAxis();
        }
    }

    private void ApplyGizmoAxis()
    {
        if (!_gizmoHit) return;
        var ray = Builder.CursorRay();
        MoveGizmo.localScale = Vector3.one * Vector3.Distance(ray.origin, MoveGizmo.position) * 0.05f;
        CalculateLineLineIntersection(_selectionPoint, _selectionPoint +_moveAxis * 100, ray.origin, ray.origin + ray.direction * 100, out Vector3 point1, out var point2);
        Vector3 dif = point1 - _intersectionPoint; // find the difference between the previous intersection point and the new intersection point 
        _intersectionPoint = point1; // update intersection point for next frame
        MoveGizmo.position += dif; // apply difference to selection
        for (int i = 0; i < selectedProps.Length; i++)
        {
            selectedProps[i].transform.position += dif;
        }
        BuildManager.PositionsDirty();
    }

    private void FindGizmoAxis()
    {
        if (_gizmoHit) // reset gizmo hit
        {
            _gizmoHit = false;
            YArrow.localScale = Vector3.one * 100f;
            XArrow.localScale = Vector3.one * 100f;
            ZArrow.localScale = Vector3.one * 100f;
        }
        
        var ray = Builder.CursorRay();
        if (selectedProps.Length == 0)
        {
            // no selections, don't bother
            MoveGizmo.gameObject.SetActive(false);
            return; 
        }
        else
        {
            // something selected, setup gizmo
            MoveGizmo.gameObject.SetActive(true);
            MoveGizmo.SetPositionAndRotation(selectedProps[0].transform.position, selectedProps[0].transform.rotation);
            MoveGizmo.localScale = Vector3.one * Vector3.Distance(ray.origin, MoveGizmo.position) * 0.05f;
        }
        // test conditions
        if (!Physics.Raycast(Builder.UIRay(), out RaycastHit hit, float.MaxValue, 1 << 5, QueryTriggerInteraction.Collide)) return;
        if (!Utilities.IsValid(hit.collider)) return;
        if (!Utilities.IsValid(hit.collider.transform)) return;
        if (!Utilities.IsValid(hit.collider.transform.parent)) return;
        if (hit.collider.gameObject.transform.parent != MoveGizmo) return;
        _gizmoHit = true; // passed!
        
        Transform hitTransform = hit.collider.transform;
        _selectionPoint = MoveGizmo.position;
        
        if (hitTransform == YArrow)
        {
            _moveAxis = MoveGizmo.rotation * Vector3.up;
            YArrow.localScale = Vector3.one * 120f;
        }
        else if (hitTransform == XArrow)
        {
            _moveAxis = MoveGizmo.rotation * Vector3.right;
            XArrow.localScale = Vector3.one * 120f;
        }
        else if (hitTransform == ZArrow)
        {
            _moveAxis = MoveGizmo.rotation * Vector3.forward;
            ZArrow.localScale = Vector3.one * 120f;
        }
        
        CalculateLineLineIntersection(_selectionPoint, _selectionPoint +_moveAxis * 100, ray.origin, ray.origin + ray.direction * 100, out Vector3 point1, out var point2);
        _intersectionPoint = point1;
    }
    
    public static bool CalculateLineLineIntersection(Vector3 line1Point1, Vector3 line1Point2, 
        Vector3 line2Point1, Vector3 line2Point2, out Vector3 resultSegmentPoint1, out Vector3 resultSegmentPoint2)
    {
        // Algorithm is ported from the C algorithm of 
        // Paul Bourke at http://local.wasp.uwa.edu.au/~pbourke/geometry/lineline3d/
        resultSegmentPoint1 = Vector3.zero;
        resultSegmentPoint2 = Vector3.zero;
 
        Vector3 p1 = line1Point1;
        Vector3 p2 = line1Point2;
        Vector3 p3 = line2Point1;
        Vector3 p4 = line2Point2;
        Vector3 p13 = p1 - p3;
        Vector3 p43 = p4 - p3;
 
        if (p43.sqrMagnitude < float.Epsilon) {
            return false;
        }
        Vector3 p21 = p2 - p1;
        if (p21.sqrMagnitude < float.Epsilon) {
            return false;
        }
 
        double d1343 = p13.x * (double)p43.x + (double)p13.y * p43.y + (double)p13.z * p43.z;
        double d4321 = p43.x * (double)p21.x + (double)p43.y * p21.y + (double)p43.z * p21.z;
        double d1321 = p13.x * (double)p21.x + (double)p13.y * p21.y + (double)p13.z * p21.z;
        double d4343 = p43.x * (double)p43.x + (double)p43.y * p43.y + (double)p43.z * p43.z;
        double d2121 = p21.x * (double)p21.x + (double)p21.y * p21.y + (double)p21.z * p21.z;
 
        double denom = d2121 * d4343 - d4321 * d4321;
        if (Math.Abs(denom) < float.Epsilon) {
            return false;
        }
        double numer = d1343 * d4321 - d1321 * d4343;
 
        double mua = numer / denom;
        double mub = (d1343 + d4321 * (mua)) / d4343;
 
        resultSegmentPoint1.x = (float)(p1.x + mua * p21.x);
        resultSegmentPoint1.y = (float)(p1.y + mua * p21.y);
        resultSegmentPoint1.z = (float)(p1.z + mua * p21.z);
        resultSegmentPoint2.x = (float)(p3.x + mub * p43.x);
        resultSegmentPoint2.y = (float)(p3.y + mub * p43.y);
        resultSegmentPoint2.z = (float)(p3.z + mub * p43.z);

        return true;
    }

}
