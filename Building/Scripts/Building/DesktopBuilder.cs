
using System;
using Phasedragon.AdminUtilities;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using VRC.SDK3.Data;
using VRC.SDK3.Rendering;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;
using VRDC_systems.Building.Scripts.Building;

public class DesktopBuilder : Builder
{
    public Camera cam;
    
    public float cursorSpeed = 10;
    public float cameraRotationSpeed = 10;
    public float cameraMoveSpeed = 10;
    public float cameraScrollSpeed = 1;

    private Vector3 grabStart;

    public Vector3 localCursorPosition => cursor.localPosition;
    public Vector3 globalCursorPosition => cursor.position;
    public override Ray CursorRay()
    {
        return new Ray(cam.transform.position, globalCursorPosition - cam.transform.position);
    }

    public override Vector3 Up()
    {
        return cam.transform.up;
    }

    public override Vector3 Forward()
    {
        return cam.transform.forward;
    }

    public override Vector3 CameraPosition()
    {
        return cam.transform.position;
    }
    
    public override void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        if (Networking.LocalPlayer.IsUserInVR())
        {
            gameObject.SetActive(false);
            return;
        }
        SelectedTool = Observable.Create(0);
        _quickMenu = QuickMenu.Instance();
        _quickMenu.RegisterEvent("Builder/Desktop build mode", this, nameof(ToggleBuild)).WithPropertyPriority(100).WithPropertyCloseAfter(true);
        buildManager = BuildManager.Instance();
        _cursorImage = cursor.GetComponentInChildren<Image>();
        foreach (var tool in builderTools)
        {
            tool.Initialize(this, buildManager);
        }
        SelectedTool.Subscribe(this, nameof(SelectedToolChanged));
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
        ActiveTool.ToolUpdate();
    }
    
    public void HandleCursor()
    {
        var moveHorizontal = Input.GetAxisRaw("Horizontal");
        var moveForward = Input.GetAxisRaw("Vertical");
        var moveVertical = (Input.GetKey(KeyCode.Space) ? 1 : 0) + (Input.GetKey(KeyCode.LeftControl) ? -1 : 0);
        var lookHorizontal = Input.GetAxisRaw("Mouse X");
        var lookVertical = Input.GetAxisRaw("Mouse Y");
        var scrollWheel = Input.GetAxisRaw("Mouse ScrollWheel");
        
        // handle key movement
        cam.transform.position += 
            (cam.transform.rotation * new Vector3(moveHorizontal, 0, moveForward) 
             + new Vector3(0,moveVertical, 0)) 
            * cameraMoveSpeed * Time.deltaTime;
        
        cam.transform.localScale = Vector3.one;


        bool hover = interactionManager.Hover(cursor.localPosition);
        _cursorImage.sprite = hover ? hoverCursor : normalCursor;
        
        if (Input.GetMouseButtonDown(0))
        {
            if (hover) interactionManager.Click(cursor.localPosition);
            else ActiveTool.PrimaryAction(true);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            if (!hover) ActiveTool.PrimaryAction(false);
        }
        
        if (!Input.GetMouseButton(1) || !Input.GetMouseButton(1)) // don't move cursor if both middle and right are held
        {
            Vector3 position = cursor.localPosition;
            position += new Vector3(lookHorizontal, lookVertical, 0) * cursorSpeed * Time.deltaTime;
            position.x = Mathf.Clamp(position.x, -canvas.rect.width / 2, canvas.rect.width / 2);
            position.y = Mathf.Clamp(position.y, -canvas.rect.height / 2, canvas.rect.height / 2);
            cursor.localPosition = position;
        }
        
        if (Input.GetMouseButton(1))
        {
            Quaternion rotation = cam.transform.rotation;
            rotation = rotation * Quaternion.Euler(-lookVertical * Time.deltaTime * cameraRotationSpeed, 0, 0);
            rotation = Quaternion.Euler(0, lookHorizontal * Time.deltaTime * cameraRotationSpeed, 0) * rotation;
            //angle.x = Mathf.Clamp(angle.x, -89, 89);
            cam.transform.rotation = rotation;
        }
        if (Input.GetMouseButton(2))
        {
            if (Input.GetMouseButtonDown(2))
            {
                Raycast(QueryTriggerInteraction.Collide, out grabStart, out var normal, out var o);
            }
            else
            {
                float distance = Vector3.Distance(cam.transform.position, grabStart);
                Vector3 projection = cam.transform.position + (Vector3.Normalize(cursor.position - cam.transform.position) * distance);
                Vector3 diff = grabStart - projection; // calculate difference
                
                // flatten difference
                diff = Quaternion.Inverse(cam.transform.rotation) * diff;
                diff.z = 0;
                diff = cam.transform.rotation * diff;
                
                // add in scroll wheel
                Vector3 grabToCam = cam.transform.position - grabStart;
                diff -= grabToCam * scrollWheel * cameraScrollSpeed * Time.deltaTime; 
                
                cam.transform.position += diff;
            }
        }
    }

    public void HandleTools()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectedTool.SetValue(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectedTool.SetValue(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectedTool.SetValue(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SelectedTool.SetValue(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SelectedTool.SetValue(4);
        if (Input.GetKeyDown(KeyCode.Alpha6)) SelectedTool.SetValue(5);
        if (!Input.GetMouseButton(1) && !Input.GetMouseButton(2))
        {
            float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
            if (scroll != 0) ActiveTool.Scroll(scroll);
        }
    }

    public void Ray(out Vector3 position, out Vector3 direction)
    {
        position = cam.transform.position;
        direction = Vector3.Normalize(cursor.position - cam.transform.position);
    }

    
    public void StopBuilding()
    {
        _active = false;
        canvas.gameObject.SetActive(_active);
        cam.enabled = _active;
    }
    public void ToggleBuild()
    {
        _active = !_active;
        canvas.gameObject.SetActive(_active);
        cam.enabled = _active;
        cam.transform.position = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position + Vector3.up;
        cam.transform.rotation = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
        ActiveTool.SetToolActive(_active);
    }
}