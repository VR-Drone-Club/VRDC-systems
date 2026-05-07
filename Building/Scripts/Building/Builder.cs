using Phasedragon.AdminUtilities;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace VRDC_systems.Building.Scripts.Building
{
    /// <summary>
    /// Builder is the base class for VRBuilder and DesktopBuilder. This gives the UI and other systa consistent interface to get information and perform actions, while allowing different platforms to have different implementations
    /// </summary>
    public class Builder : UdonSharpBehaviour
    {
        
        public RectTransform canvas;
        public BuildManager buildManager;
        public BuilderTool[] builderTools;
        public InteractionManager interactionManager;
        public Sprite normalCursor;
        public Sprite hoverCursor;
        public Transform cursor;
        
        internal Image _cursorImage;
        internal QuickMenu _quickMenu;
        internal bool _initialized;
        internal bool _active;

        private DataList _toolObjectKeys = new DataList();
        private DataList _toolObjects = new DataList();

        
        public virtual Ray CursorRay()
        {
            return new Ray();
        }

        public virtual Plane CanvasPlane()
        {
            return new Plane(canvas.forward, canvas.position);
        }

        public virtual Vector3 Up()
        {
            return Vector3.up;
        }

        public virtual Vector3 Forward()
        {
            return Vector3.up;
        }

        public virtual Vector3 CameraPosition()
        {
            return Vector3.zero;
        }

        public BuilderTool ActiveTool
        {
            get
            {
                if (SelectedTool.GetInt() >= builderTools.Length) SelectedTool.SetValue(builderTools.Length - 1);
                return builderTools[SelectedTool.GetInt()];
            }
        } 
        private DataList _selectedTool;
        public Observable SelectedTool
        {
            get
            {
                if (_selectedTool == null) _selectedTool = Observable.Create(0);
                return (Observable)_selectedTool;
            }
            set => _selectedTool = value;
        }

        public virtual void Initialize()
        {
            
        }

        public bool Raycast(QueryTriggerInteraction queryTriggerInteraction, out Vector3 position, out Vector3 normal, out GameObject gameObject)
        {
            return Raycast(CursorRay(), queryTriggerInteraction, out position, out normal, out gameObject);
        }
        public bool Raycast(Ray ray, QueryTriggerInteraction queryTriggerInteraction, out Vector3 position, out Vector3 normal, out GameObject gameObject)
        {
            if (!Physics.Raycast(ray, out RaycastHit hit, 10000, 1, queryTriggerInteraction))
            {
                position = Vector3.zero;
                normal = Vector3.up;
                gameObject = null;
                return false;
            }
        
            position = hit.point + (hit.normal * 0.05f);
        
            position = buildManager.RoundPosition(position);
            normal = hit.normal;
            if (Utilities.IsValid(hit.collider))
                gameObject = hit.collider.gameObject;
            else
                gameObject = null;
        
            return true;
        }

        public void AddToolObject(string key, GameObject go)
        {
            _toolObjectKeys.Add(key);
            _toolObjects.Add(go);
            ApplyToolObjects();
        }
        public void SelectedToolChanged()
        {
            for (int i = 0; i < builderTools.Length; i++)
            {
                builderTools[i].SetToolActive(i == SelectedTool.GetInt());
            }
            ApplyToolObjects();
        }

        private void ApplyToolObjects()
        {
            // disable all first
            for (int i = 0; i < _toolObjects.Count && i < _toolObjectKeys.Count; i++)
            {
                ((GameObject)_toolObjects[i].Reference).SetActive(false);
            }
            // enable any that match
            for (int i = 0; i < _toolObjects.Count && i < _toolObjectKeys.Count; i++)
            {
                if (_toolObjectKeys[i].String == ActiveTool.name) ((GameObject)_toolObjects[i].Reference).SetActive(true);
            }
        }

        public Vector3 LocalCursorPosition()
        {
            Ray ray = CursorRay();
            Plane plane = CanvasPlane();
            plane.Raycast(ray, out float enter);
            return WorldToCanvasPoint(ray.GetPoint(enter));
        }

        public Vector3 WorldToCanvasPoint(Vector3 input)
        {
            Plane plane = CanvasPlane();
            Ray rayToPoint = new Ray(input, CameraPosition() - input);
            plane.Raycast(rayToPoint, out float enter);
            Vector3 pointOnPlane = rayToPoint.GetPoint(enter);
            Vector3 localPoint = canvas.InverseTransformPoint(pointOnPlane);
            localPoint.z = 0;
            return localPoint;
        }

        public Vector3 HoverPoint()
        {
            var ray = CursorRay();
            CanvasPlane().Raycast(ray, out float enter);
            return WorldToCanvasPoint(ray.GetPoint(enter));
        }

        public Ray UIRay()
        {
            var ray = CursorRay();
            CanvasPlane().Raycast(ray, out float enter);
            return new Ray(CameraPosition(), ray.GetPoint(enter) - CameraPosition());
        }

        public virtual void SetUIScale()
        {
            
        }
    }
}