
using System;
using UdonSharp;
using UdonToolkit;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;
using VRC.Udon;
using VRCLightVolumes;

#if UNITY_EDITOR
[CustomEditor(typeof(ColorApplicator))]
public class ColorApplicatorInspector : UTEditor{}
#endif
public class ColorApplicator : UdonSharpBehaviour
{
    public bool applyAutomatically;
    public ColorMode colorMode;
    public ColorPicker colorPicker;
    public Renderer[] _renderers;
    public ParticleSystem[] _particleSystems;
    public TrailRenderer[] _trailRenderers;
    public bool findLightVolumes;
    public LightVolumeInstance[] lightVolumes;
    private Color _primaryColor;
    private Color _secondaryColor;
    private Color _effectColor;
    private void OnEnable()
    {
        if (applyAutomatically) Apply(Networking.GetOwner(gameObject));
    }

    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        if (applyAutomatically) Apply(player);
    }

    [Button("Preview")]
    public void Preview()
    {
        var gradient = new Gradient();
        gradient.mode = GradientMode.Blend;
        gradient.colorKeys = new GradientColorKey[]
        {
            new GradientColorKey(Color.white, 0.1f),
            new GradientColorKey(Color.cyan, 0.15f),
        };
        /*
        gradient.colorKeys[0].color = Color.white;
        gradient.colorKeys[0].time = 0.1f;
        gradient.colorKeys[1].color = Color.cyan;
        gradient.colorKeys[1].time = 0.15f;*/
        gradient.alphaKeys = new GradientAlphaKey[]
        {
            new GradientAlphaKey(1, 0),
            new GradientAlphaKey(0, 1)
        };
        for (int i = 0; i < _trailRenderers.Length; i++)
        {
            if (Utilities.IsValid(_trailRenderers[i])) _trailRenderers[i].colorGradient = gradient;
        }
    }
    
    public void Apply(VRCPlayerApi player)
    {
        if (!Utilities.IsValid(colorPicker)) colorPicker = ColorPicker.Instance();
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (Utilities.IsValid(_renderers[i])) colorPicker.AssignRenderer(player, _renderers[i]);
        }

        for (int i = 0; i < _particleSystems.Length; i++)
        {
            if (Utilities.IsValid(_particleSystems[i])) colorPicker.AssignParticleSystem(player, _particleSystems[i]);
        }

        for (int i = 0; i < _trailRenderers.Length; i++)
        {
            if (Utilities.IsValid(_trailRenderers[i])) colorPicker.AssignTrailRenderer(player, _trailRenderers[i]);
        }

        if (findLightVolumes) lightVolumes = GetComponentsInChildren<LightVolumeInstance>();
        for (int i = 0; i < lightVolumes.Length; i++)
        {
            if (Utilities.IsValid(lightVolumes[i])) colorPicker.AssignLightVolume(player, lightVolumes[i]);
        }
    }
}
