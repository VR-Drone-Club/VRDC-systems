
using System;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class FogController : UdonSharpBehaviour
{
    public MeshRenderer playerFogRenderer;
    public LayerMask cloudLayer;
    public float maxRadius;
    public float minRadius;

    private float currentFade;
    private Collider[] _colliders = new Collider[1];
    private MaterialPropertyBlock _fogBlock;
    private Collider _lastCloudCopied;
    private void Start()
    {
        _fogBlock = new MaterialPropertyBlock();
    }

    private void Update()
    {
        VRCPlayerApi.TrackingData trackingData = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
        transform.position = trackingData.position;
        TestPosition(trackingData.position);
        ApplyFade();
    }

    private void TestPosition(Vector3 position)
    {
        float currentRadius = Mathf.Lerp(maxRadius, minRadius, currentFade);
        _colliders[0] = null;
        Physics.OverlapSphereNonAlloc(position, currentRadius, _colliders, cloudLayer, QueryTriggerInteraction.Collide);
        if (Utilities.IsValid(_colliders[0]) && _colliders[0] != _lastCloudCopied)
        {
            CopyColors(_colliders[0]);
        }
        float fadeSpeed = Utilities.IsValid(_colliders[0]) ? 1f : -1f;
        fadeSpeed *= Time.deltaTime;
        currentFade += fadeSpeed;
        currentFade = Mathf.Clamp(currentFade, 0, 1);
    }

    private void ApplyFade()
    {
        playerFogRenderer.GetPropertyBlock(_fogBlock);
        _fogBlock.SetFloat("_ManualFade", currentFade);
        playerFogRenderer.SetPropertyBlock(_fogBlock);
    }

    private void CopyColors(Collider collider)
    {
        _lastCloudCopied = collider;
        Renderer renderer = collider.GetComponent<Renderer>();
        var material = renderer.sharedMaterial;
        playerFogRenderer.GetPropertyBlock(_fogBlock);
        _fogBlock.SetFloat("_DepthTransp", material.GetFloat("_DepthTransp"));
        _fogBlock.SetFloat("_HeightBottom", material.GetFloat("_HeightBottom"));
        _fogBlock.SetFloat("_HeightMiddle", material.GetFloat("_HeightMiddle"));
        _fogBlock.SetFloat("_HeightTop", material.GetFloat("_HeightTop"));
        _fogBlock.SetColor("_ColorBottom", material.GetColor("_ColorBottom"));
        _fogBlock.SetColor("_ColorMiddle", material.GetColor("_ColorMiddle"));
        _fogBlock.SetColor("_ColorTop", material.GetColor("_ColorTop"));
        playerFogRenderer.SetPropertyBlock(_fogBlock);
    }
}
