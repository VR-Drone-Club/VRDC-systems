
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ColorApplicator : UdonSharpBehaviour
{
    public ColorMode colorMode;
    public ColorPicker colorPicker;
    public Renderer _renderer;
    public ParticleSystem _particleSystem;
    private Color _primaryColor;
    private Color _secondaryColor;
    private Color _effectColor;
    void Start()
    {
        if (!Utilities.IsValid(colorPicker)) colorPicker = ColorPicker.Instance();
        colorPicker.AssignRenderer(Networking.GetOwner(gameObject), _renderer);
        colorPicker.AssignParticleSystem(Networking.GetOwner(gameObject), _particleSystem);
    }
    

    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        colorPicker.AssignRenderer(Networking.GetOwner(gameObject), _renderer);
        colorPicker.AssignParticleSystem(Networking.GetOwner(gameObject), _particleSystem);
    }
}
