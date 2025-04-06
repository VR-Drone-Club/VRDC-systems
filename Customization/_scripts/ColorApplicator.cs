
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ColorApplicator : UdonSharpBehaviour
{
    public ColorMode colorMode;
    public ColorPicker colorPicker;
    private Renderer _renderer;
    private ParticleSystem _particleSystem;
    private Color _primaryColor;
    private Color _secondaryColor;
    private Color _effectColor;
    void Start()
    {
        if (!Utilities.IsValid(colorPicker)) colorPicker = ColorPicker.Instance();
        _renderer = GetComponent<Renderer>();
        _particleSystem = GetComponent<ParticleSystem>();
        colorPicker.SubscribeToChanges(Networking.LocalPlayer, this); // should change this to subscribe the renderer and particlesystem straight to the colorpicker, would be good for performance
    }

    public void ColorChanged()
    {
        Debug.Log("Colorpicker says color was changed");
        VRCPlayerApi player = Networking.GetOwner(gameObject);
        _primaryColor = colorPicker.GetPrimary(player);
        _secondaryColor = colorPicker.GetSecondary(player);
        _effectColor = colorPicker.GetEffect(player);

        Apply();
    }

    public void Apply()
    {
        if (Utilities.IsValid(_particleSystem))
        {
            var main = _particleSystem.main;
            var alpha = main.startColor.Evaluate(0).a;
            switch (colorMode)
            {
                case ColorMode.Primary:
                    main.startColor = new Color(_primaryColor.r, _primaryColor.g, _primaryColor.b, alpha);
                    break;
                case ColorMode.Secondary:
                    main.startColor = new Color(_secondaryColor.r, _secondaryColor.g, _secondaryColor.b, alpha);
                    break;
                case ColorMode.Effect:
                    main.startColor = new Color(_effectColor.r, _effectColor.g, _effectColor.b, alpha);
                    break;
            }
        }

        if (Utilities.IsValid(_renderer))
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor("_Color0", _primaryColor * Random.Range(0.8f, 1.2f));
            block.SetColor("_Color1", _secondaryColor * Random.Range(0.8f, 1.2f));
            block.SetColor("_EmissionColor", _effectColor * Random.Range(0.8f, 1.2f));
            _renderer.SetPropertyBlock(block);
        }
    }
}
