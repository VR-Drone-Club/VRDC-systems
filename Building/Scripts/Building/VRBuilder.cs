
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
    }
    
    private void LateUpdate()
    {
        if (!_initialized) return;

        if (Input.GetKeyDown(KeyCode.B))
        {
            ToggleBuild();
        }

        if (!_active) return;

        canvasPivot.SetPositionAndRotation(VRCCameraSettings.ScreenCamera.Position, VRCCameraSettings.ScreenCamera.Rotation);
        HandleCursor();
        HandleTools();
        ActiveTool.ToolUpdate();
    }

    public override void InputGrab(bool value, UdonInputEventArgs args)
    {
        if (!_active) return;
        if (_lastHand != args.handType)
        {
            _lastHand = args.handType;
            return;
        }
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
