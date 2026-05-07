
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;
using VRDC_systems.Building.Scripts.Building;

public class AltitudeBuild : BuilderTool
{
    public Transform grid;
    private DataList _templates;
    private Transform _preview;

    private string SelectedTemplate
    {
        get => Properties["SelectedTemplate"].DataList.AsObservable().GetString();
        set => Properties["SelectedTemplate"].DataList.AsObservable().SetValue(value);
    } 
    public override void Initialize(Builder builder, BuildManager buildManager)
    {
        base.Initialize(builder, buildManager);
        _templates = buildManager.GetTemplates();
        Properties["SelectedTemplate"] = Observable.Create(string.Empty);
        Properties["SelectedTemplate"].DataList.AsObservable().Subscribe(this, nameof(TemplateChanged));
    }

    public override void Scroll(float change)
    {
        int current = _templates.IndexOf(SelectedTemplate);
        if (change > 0)
        {
            current++;
            if (current >= _templates.Count) current = 0;
        }
        if (change < 0)
        {
            current--;
            if (current < 0) current = _templates.Count - 1;
        }

        SelectedTemplate = _templates[current].String;
    }

    public void TemplateChanged()
    {
        if (Utilities.IsValid(_preview)) Destroy(_preview.gameObject);
    }

    public override void SetToolActive(bool active)
    {
        if (!active && Utilities.IsValid(_preview)) Destroy(_preview.gameObject);
    }

    public override void ToolUpdate()
    {
        if (!Utilities.IsValid(_preview))
        {
            _preview = BuildManager.GetPropPreview(SelectedTemplate);
            if (Utilities.IsValid(_preview))
            {
                
                var components = _preview.GetComponentsInChildren<Collider>(true);
                foreach (var component in components)
                {
                    component.enabled = false;
                }
            }
        }

        GetPlacementLocation(out Vector3 position, out Quaternion rotation);
        if (Utilities.IsValid(_preview)) _preview.SetPositionAndRotation(position, rotation);
    }

    public override void PrimaryAction(bool down)
    {
        if (!down) return;
        GetPlacementLocation(out Vector3 position, out Quaternion rotation);
        BuildManager.SpawnPropSynced(SelectedTemplate, position, rotation);
    }

    private bool GetPlacementLocation(out Vector3 position, out Quaternion rotation)
    {
        Ray cursor = Builder.CursorRay();
        Plane plane = new Plane(Vector3.right, Vector3.forward, Vector3.left);
        plane.normal = Vector3.up;
        plane.distance = -10;
        plane.Raycast(cursor, out float intersect);
        position = cursor.GetPoint(intersect);
        
        rotation = Quaternion.LookRotation(plane.normal, Builder.Forward()) * Quaternion.Euler(new Vector3(-90, 180, 0));
        position = BuildManager.RoundPosition(position);
        rotation = BuildManager.RoundRotation(rotation);
        return true;
    }
}
