
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class EffectPickerMenu : UdonSharpBehaviour
{
    public EffectPicker effectPicker;
    public TextMeshProUGUI trailText;
    public TextMeshProUGUI burstText;
    public Animator trailPreviewAnimator;
    
    public override void OnPlayerRestored(VRCPlayerApi player)
    {
        trailText.text = effectPicker.GetTrailName(Networking.LocalPlayer);
        burstText.text = effectPicker.GetBurstName(Networking.LocalPlayer);
    }

    public void NextTrail()
    {
        effectPicker.IncrementTrail(1);
        trailText.text = effectPicker.GetTrailName(Networking.LocalPlayer);
    }

    public void PrevTrail()
    {
        effectPicker.IncrementTrail(-1);
        trailText.text = effectPicker.GetTrailName(Networking.LocalPlayer);
    }

    public void NextBurst()
    {
        effectPicker.IncrementBurst(1);
        burstText.text = effectPicker.GetBurstName(Networking.LocalPlayer);
    }

    public void PrevBurst()
    {
        effectPicker.IncrementBurst(-1);
        burstText.text = effectPicker.GetBurstName(Networking.LocalPlayer);
    }

    public void PreviewTrail()
    {
        trailPreviewAnimator.SetTrigger("Activate");
    }

    public void PreviewBurst()
    {
        effectPicker.Burst(Networking.LocalPlayer, transform.position, transform.rotation);
    }
}
