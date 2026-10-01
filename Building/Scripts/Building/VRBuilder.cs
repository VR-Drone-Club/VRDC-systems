
using BobyStar.DualLaser;
using Phasedragon.AdminUtilities;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Rendering;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using VRDC_systems.Building.Scripts.Building;

public class VRBuilder : Builder
{
    public DualLaser DualLaser;
    public Transform canvasPivot;
    public PlayerProperties playerProperties;
    public PlayerStatusController playerStatusController;
    
    void Start()
    {
        
    }
    
    public override void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        if (!Networking.LocalPlayer.IsUserInVR())
        {
            gameObject.SetActive(false);
            return;
        }
        SelectedTool = Observable.Create(0);
        _quickMenu = QuickMenu.Instance();
        _quickMenu.RegisterEvent("Builder/VR build mode", this, nameof(ToggleBuild)).WithPropertyPriority(100).WithPropertyCloseAfter(true);
        buildManager = BuildManager.Instance();
        _cursorImage = cursor.GetComponentInChildren<Image>();
        foreach (var tool in builderTools)
        {
            tool.Initialize(this, buildManager);
        }
        SelectedTool.Subscribe(this, nameof(SelectedToolChanged));
    }

    public void ToggleBuild()
    {
        Initialize();
        _active = !_active;
        canvas.gameObject.SetActive(_active);
        ActiveTool.SetToolActive(_active);
        buildManager.SetEditing(_active);
        if (_active)
        {
            playerStatusController.AddStatus("noclip", 10f, -1, true);
        }
        else
        {
            playerStatusController.RemoveStatus("noclip", 10f, true);
        }
    }
    
    private void Update()
    {
        if (!_initialized) return;

        if (Input.GetKeyDown(KeyCode.B))
        {
            ToggleBuild();
        }

        if (!_active) return;

        HandleCursor();
        HandleTools();
        HandleGrab();
        ActiveTool.ToolUpdate();
        canvasPivot.SetPositionAndRotation(VRCCameraSettings.ScreenCamera.Position, VRCCameraSettings.ScreenCamera.Rotation);
    }


    private HandType _lastHand;
    private bool _lastClickedUI;
    
    public override void InputUse(bool value, VRC.Udon.Common.UdonInputEventArgs args)
    {
        if (!_active) return;
        if (_lastHand != args.handType)
        {
            _lastHand = args.handType;
            return;
        }
        var hoverPoint = HoverPoint();
        bool hover = interactionManager.Hover(hoverPoint);
        if (hover && value)
        {
            interactionManager.Click(hoverPoint);
            _lastClickedUI = true; // set so that the release action from a UI event doesn't trigger PrimaryAction
        }
        else if (!_lastClickedUI)
        {
            ActiveTool.PrimaryAction(value);
        }
        else
        {
            _lastClickedUI = false;
        }
    }
    
    private void HandleCursor()
    {
        var hoverPoint = HoverPoint();
        bool uiHover = interactionManager.Hover(hoverPoint);
        _cursorImage.sprite = uiHover ? hoverCursor : normalCursor;
        bool worldHit = Raycast(QueryTriggerInteraction.Collide, out Vector3 position, out Vector3 normal, out GameObject go);

        if (uiHover || !worldHit || ActiveTool.name == "SelectTool")
        {
            cursor.localPosition = hoverPoint; // if we're hovering over a UI element, use the UI hover point
        }
        else // otherwise, use the world hit point
        {
            cursor.localPosition = WorldToCanvasPoint(position);
        }
    }
    private void HandleTools()
    {
        if (_leftGrab || _rightGrab) return;
        ActiveTool.Scroll(-5 * _lookVertical * Time.deltaTime);
    }

    private float _lookVertical;
    public override void InputLookVertical(float value, UdonInputEventArgs args)
    {
        _lookVertical = value;
        base.InputLookVertical(value, args);
    }

    private bool _leftGrab;
    private Vector3 _leftGrabPoint;
    private float _leftGrabDistance;
    private bool _rightGrab;
    private Vector3 _rightGrabPoint;
    private float _rightGrabDistance;
    
    public override void InputGrab(bool value, UdonInputEventArgs args)
    {
        if (!_active) return;
        var ray = DualLaser.GetPointerRay(args.handType);
        Raycast(ray, QueryTriggerInteraction.Collide, out Vector3 pos, out Vector3 normal, out GameObject o);
        if (args.handType == HandType.LEFT)
        {
            if (value) _rightGrab = false;
            _leftGrab = value;
            _leftGrabPoint = pos;
            _leftGrabDistance = Vector3.Distance(ray.origin, _leftGrabPoint);
        }
        else
        {
            if (value) _leftGrab = false;
            _rightGrab = value;
            _rightGrabPoint = pos;
            _rightGrabDistance = Vector3.Distance(ray.origin, _rightGrabPoint);
        }
    }
    
    public float builderScrollSpeed = -5;
    private void HandleGrab()
    {
        //Networking.LocalPlayer.SetVelocity(Vector3.zero);
        var scroll = _lookVertical * Time.deltaTime * builderScrollSpeed;
        _leftGrabDistance = Mathf.Max(0, _leftGrabDistance + scroll);
        _rightGrabDistance = Mathf.Max(0, _rightGrabDistance + scroll);
        if (_leftGrab)
        {
            var ray = DualLaser.GetPointerRay(HandType.LEFT);
            var intersect = ray.GetPoint(_leftGrabDistance);
            Vector3 difference = _leftGrabPoint - intersect;
            var origin = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin);
            var target = Vector3.Lerp(origin.position, origin.position + difference, 1f);
            playerProperties.TeleportTo(target, origin.rotation);
        }
        if (_rightGrab)
        {
            var ray = DualLaser.GetPointerRay(HandType.RIGHT);
            var intersect = ray.GetPoint(_rightGrabDistance);
            Vector3 difference = _rightGrabPoint - intersect;
            var origin = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin);
            var target = Vector3.Lerp(origin.position, origin.position + difference, 1f);
            playerProperties.TeleportTo(target, origin.rotation);
        }
    }

    public override Ray CursorRay()
    {
        return DualLaser.GetPointerRay();
    }

    public override Vector3 Up()
    {
        return VRCCameraSettings.ScreenCamera.Up;
    }

    public override Vector3 Forward()
    {
        return VRCCameraSettings.ScreenCamera.Forward;
    }

    public override Vector3 CameraPosition()
    {
        return VRCCameraSettings.ScreenCamera.Position;
    }
}
