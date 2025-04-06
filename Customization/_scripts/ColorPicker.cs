
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDK3.Persistence;
using VRC.SDKBase;
using VRC.Udon;

public class ColorPicker : UdonSharpBehaviour
{
    private DataList _subscribedBehaviors = new DataList();
    private DataList _subscribedBehaviourPlayers = new DataList();
    private DataDictionary _primaryColors = new DataDictionary();
    private DataDictionary _secondaryColors = new DataDictionary();
    private DataDictionary _effectColors = new DataDictionary();

    public static ColorPicker Instance()
    {
        GameObject colorPickerObj = GameObject.Find("ColorPicker");
        if (!Utilities.IsValid(colorPickerObj)) return null;
        return colorPickerObj.GetComponent<ColorPicker>();
    }
    private Color[] _defaultColors = new Color[]
    {
        Color.HSVToRGB(0, 1, 1),
        Color.HSVToRGB(0.1f, 1, 1),
        Color.HSVToRGB(0.15f, 1, 1),
        Color.HSVToRGB(0.3f, 1, 1),
        Color.HSVToRGB(0.5f, 1, 1),
        Color.HSVToRGB(0.6f, 1, 1),
        Color.HSVToRGB(0.75f, 1, 1),
        Color.HSVToRGB(0.8f, 1, 1),
    };

    public override void OnPlayerRestored(VRCPlayerApi player)
    {
        ReadOrAssignColors(player);
    }

    public override void OnPlayerDataUpdated(VRCPlayerApi player, PlayerData.Info[] infos)
    {
        ReadOrAssignColors(player);
    }

    private void ReadOrAssignColors(VRCPlayerApi player)
    {
        Color defaultColor = _defaultColors[player.playerId % _defaultColors.Length];
        
        _primaryColors[player.displayName] = PlayerData.TryGetColor(player, "Color_Primary", out Color primaryColor) 
            ? new DataToken(primaryColor) // read player's color preference if they have one
            : new DataToken(defaultColor); // otherwise, assign a default color
        
        _secondaryColors[player.displayName] = PlayerData.TryGetColor(player, "Color_Secondary", out Color secondaryColor) 
            ? new DataToken(secondaryColor) // read player's color preference if they have one
            : new DataToken(defaultColor * 0.8f); // otherwise, assign a default color
                
        _effectColors[player.displayName] = PlayerData.TryGetColor(player, "Color_Effect", out Color effectColor) 
            ? new DataToken(effectColor) // read player's color preference if they have one
            : new DataToken(defaultColor * 1.2f); // otherwise, assign a default color
        
        Debug.Log("Finished readorassigncolors");
        ColorChanged(player);
    }
    
    public void SetPrimary(Color color)
    {
        PlayerData.SetColor("Color_Primary", color);
    }

    public void SetSecondary(Color color)
    {
        PlayerData.SetColor("Color_Secondary", color);
    }

    public void SetEffect(Color color)
    {
        PlayerData.SetColor("Color_Effect", color);
    }

    public Color GetPrimary(VRCPlayerApi player)
    {
        if (_primaryColors.ContainsKey(player.displayName)) return (Color)_primaryColors[player.displayName].Reference;
        return Color.white;
    }

    public Color GetSecondary(VRCPlayerApi player)
    {
        if (_secondaryColors.ContainsKey(player.displayName)) return (Color)_secondaryColors[player.displayName].Reference;
        return Color.white;
    }

    public Color GetEffect(VRCPlayerApi player)
    {
        if (_effectColors.ContainsKey(player.displayName)) return (Color)_effectColors[player.displayName].Reference;
        return Color.white;
    }
    
    public void SubscribeToChanges(VRCPlayerApi player, UdonSharpBehaviour behaviour)
    {
        _subscribedBehaviors.Add(new DataToken(behaviour));
        _subscribedBehaviourPlayers.Add(player.displayName);
        if (_primaryColors.ContainsKey(player.displayName)) behaviour.SendCustomEvent("ColorChanged"); // if color has already been determined, send message now
    }
    
    public void ColorChanged(VRCPlayerApi player)
    {
        for (int i = 0; i < _subscribedBehaviors.Count; i++)
        {
            var key = _subscribedBehaviourPlayers[i];
            if (key != player.displayName) continue;
            if (_subscribedBehaviors[i].IsNull) continue;
            UdonSharpBehaviour behaviour = (UdonSharpBehaviour)_subscribedBehaviors[i].Reference;
            behaviour.SendCustomEvent("ColorChanged");
            Debug.Log($"Sent colorchanged event to {behaviour.name}");
        }
        Debug.Log("Finished sending colorchanged to all relevant behaviours");
    }
}