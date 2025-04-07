
using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public enum ColorMode
{
    Primary,
    Secondary,
    Effect
}
public class ColorPickerMenu : UdonSharpBehaviour
{
    public ColorPicker colorPicker;
    public Image primaryPreview;
    public Image secondaryPreview;
    public Image effectPreview;
    public Image primaryBorder;
    public Image secondaryBorder;
    public Image effectBorder;

    public Slider hueSlider;
    public Slider saturationSlider;
    public Slider valueSlider;

    private ColorMode _currentMode = ColorMode.Primary;
    private Color _primary;
    private Color _secondary;
    private Color _effect;

    private void Start()
    {
        colorPicker.AssignBehaviour(Networking.LocalPlayer, this);
    }

    public void ColorChanged()
    {
        _primary = colorPicker.GetPrimary(Networking.LocalPlayer);
        _secondary = colorPicker.GetSecondary(Networking.LocalPlayer);
        _effect = colorPicker.GetEffect(Networking.LocalPlayer);
        ChangeMode(_currentMode);
        primaryPreview.color = _primary;
        secondaryPreview.color = _secondary;
        effectPreview.color = _effect;
    }

    public void ChangeMode(ColorMode mode)
    {
        _currentMode = mode;
        Color color = _primary;
        switch (_currentMode)
        {
            case ColorMode.Primary:
                color = _primary;
                break;
            case ColorMode.Secondary:
                color = _secondary;
                break;
            case ColorMode.Effect:
                color = _effect;
                break;
        }
        primaryBorder.gameObject.SetActive(_currentMode == ColorMode.Primary);
        secondaryBorder.gameObject.SetActive(_currentMode == ColorMode.Secondary);
        effectBorder.gameObject.SetActive(_currentMode == ColorMode.Effect);
        Color.RGBToHSV(color, out float h, out float s, out float v);
        hueSlider.SetValueWithoutNotify(h);
        saturationSlider.SetValueWithoutNotify(s);
        valueSlider.SetValueWithoutNotify(v);
    }
    
    public void SetModePrimary() // Sent by button
    {
        ChangeMode(ColorMode.Primary);
    }

    public void SetModeSecondary() // Sent by button
    {
        ChangeMode(ColorMode.Secondary);
    }

    public void SetModeEffect() // Sent by button
    {
        ChangeMode(ColorMode.Effect);
    }
    
    public void SliderChanged() // Sent by sliders
    {
        float h = hueSlider.value;
        float s = saturationSlider.value;
        float v = valueSlider.value;
        switch (_currentMode)
        {
            case ColorMode.Primary:
                colorPicker.SetPrimary(Color.HSVToRGB(h,s,v));
                break;
            case ColorMode.Secondary:
                colorPicker.SetSecondary(Color.HSVToRGB(h,s,v));
                break;
            case ColorMode.Effect:
                colorPicker.SetEffect(Color.HSVToRGB(h,s,v));
                break;
        }
    }

    public void Randomize()
    {
        float randomHue = UnityEngine.Random.Range(0f, 1f);
        float randomSaturation = UnityEngine.Random.Range(0.5f, 1f);
        float randomValue = UnityEngine.Random.Range(0.5f, 1f);
        Debug.Log($"{randomHue} {randomSaturation} {randomValue}");
        Color color = Color.HSVToRGB(randomHue,randomSaturation,randomValue);
        colorPicker.SetPrimary(color);
        colorPicker.SetSecondary(color * 0.8f);
        colorPicker.SetEffect(color * 1.2f);
    }
}
