
using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;
using VRC.Udon;

public class ColorApplicator : UdonSharpBehaviour
{
    public ColorMode colorMode;
    public ColorPicker colorPicker;
    public Renderer[] _renderers;
    public ParticleSystem[] _particleSystems;
    public TrailRenderer[] _trailRenderers;
    private Color _primaryColor;
    private Color _secondaryColor;
    private Color _effectColor;
    private VRCPlayerApi _playerOverride;

    public void SetPlayer(VRCPlayerApi player)
    {
        _playerOverride = player;
        Apply(player);
    }

    private void OnEnable()
    {
        Apply(Networking.GetOwner(gameObject));
    }

    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        Apply(player);
    }

    public void Apply(VRCPlayerApi player)
    {
        if (Utilities.IsValid(_playerOverride)) player = _playerOverride;
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
    }
}
