
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class CloudData : UdonSharpBehaviour
{
    public Transform worldTransform;
    public Material defaultSettings;
    
    private int _worldMatrixId;
    private int _densityId;
    private int _colorBottomId;
    private int _colorTopId;
    private int _heightBottomId;
    private int _heightMiddleId;
    private int _heightTopId;
    
    void Start()
    {
        _worldMatrixId = VRCShader.PropertyToID("_Udon_WorldTransformMatrix");
        _densityId = VRCShader.PropertyToID("_Udon_DepthTransp");
        _colorBottomId = VRCShader.PropertyToID("_Udon_ColorBottom");
        _colorTopId = VRCShader.PropertyToID("_Udon_ColorTop");
        _heightBottomId = VRCShader.PropertyToID("_Udon_HeightBottom");
        _heightMiddleId = VRCShader.PropertyToID("_Udon_HeightMiddle");
        _heightTopId = VRCShader.PropertyToID("_Udon_HeightTop");

        if (defaultSettings != null)
        {
            VRCShader.SetGlobalFloat(_densityId, defaultSettings.GetFloat("_DepthTransp"));
            VRCShader.SetGlobalColor(_colorBottomId, defaultSettings.GetColor("_ColorBottom"));
            VRCShader.SetGlobalColor(_colorTopId, defaultSettings.GetColor("_ColorTop"));
            VRCShader.SetGlobalFloat(_heightBottomId, defaultSettings.GetFloat("_HeightBottom"));
            VRCShader.SetGlobalFloat(_heightMiddleId, defaultSettings.GetFloat("_HeightMiddle"));
            VRCShader.SetGlobalFloat(_heightTopId, defaultSettings.GetFloat("_HeightTop"));
        }
    }

    public override void PostLateUpdate()
    {
        VRCShader.SetGlobalMatrix(_worldMatrixId, worldTransform.worldToLocalMatrix);
    }

    public void _SetCloudDensity(float value)
    {
        VRCShader.SetGlobalFloat(_densityId, value);
    }
    
    public void _SetCloudColorBottom(Color value)
    {
        VRCShader.SetGlobalColor(_colorBottomId, value);
    }
    
    public void _SetCloudColorTop(Color value)
    {
        VRCShader.SetGlobalColor(_colorTopId, value);
    }
    
    public void _SetCloudHeightBottom(float value)
    {
        VRCShader.SetGlobalFloat(_heightBottomId, value);
    }
    
    public void _SetCloudHeightTop(float value)
    {
        VRCShader.SetGlobalFloat(_heightTopId, value);
    }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (Application.isPlaying) return;
        if (defaultSettings == null) return;
        //Debug.Log("Setting cloud data from default settings");
        
        _worldMatrixId = Shader.PropertyToID("_Udon_WorldTransformMatrix");
        _densityId = Shader.PropertyToID("_Udon_DepthTransp");
        _colorBottomId = Shader.PropertyToID("_Udon_ColorBottom");
        _colorTopId = Shader.PropertyToID("_Udon_ColorTop");
        _heightBottomId = Shader.PropertyToID("_Udon_HeightBottom");
        _heightMiddleId = VRCShader.PropertyToID("_Udon_HeightMiddle");
        _heightTopId = Shader.PropertyToID("_Udon_HeightTop"); 
        Shader.SetGlobalMatrix(_worldMatrixId, worldTransform.worldToLocalMatrix);

        if (defaultSettings != null)
        {
            Shader.SetGlobalFloat(_densityId, defaultSettings.GetFloat("_DepthTransp"));
            Shader.SetGlobalColor(_colorBottomId, defaultSettings.GetColor("_ColorBottom"));
            Shader.SetGlobalColor(_colorTopId, defaultSettings.GetColor("_ColorTop"));
            Shader.SetGlobalFloat(_heightBottomId, defaultSettings.GetFloat("_HeightBottom"));
            Shader.SetGlobalFloat(_heightMiddleId, defaultSettings.GetFloat("_HeightMiddle"));
            Shader.SetGlobalFloat(_heightTopId, defaultSettings.GetFloat("_HeightTop"));
        }
    }
#endif
    
}
