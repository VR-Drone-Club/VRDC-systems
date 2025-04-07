
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDK3.Persistence;
using VRC.SDKBase;
using VRC.Udon;

public class ColorPicker : UdonSharpBehaviour
{
    private DataDictionary _assignedRenderers = new DataDictionary();
    private DataDictionary _assignedParticleSystems = new DataDictionary();
    private DataDictionary _assignedBehaviours = new DataDictionary();
    private DataDictionary _inverseAssignmentMap = new DataDictionary();
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
        
        Debug.Log($"Finished readorassigncolors for {player.displayName}");
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

    public void AssignRenderer(VRCPlayerApi player, Renderer renderer)
    {
        Remove(renderer);
        string displayName = player.displayName;
        if (!_assignedRenderers.ContainsKey(player.displayName))
        {
            _assignedRenderers[displayName] = new DataList();
        }
        DataList dataList = _assignedRenderers[displayName].DataList;

        if (dataList.Contains(renderer)) return;
        dataList.Add(renderer);
        renderer.SetPropertyBlock(GetPropertyBlock(player));
        _inverseAssignmentMap[renderer] = displayName;
    }

    public void AssignParticleSystem(VRCPlayerApi player, ParticleSystem particleSystem)
    {
        Remove(particleSystem);
        string displayName = player.displayName;
        if (!_assignedParticleSystems.ContainsKey(player.displayName))
        {
            _assignedParticleSystems[displayName] = new DataList();
        }
        DataList dataList = _assignedParticleSystems[displayName].DataList;

        if (dataList.Contains(particleSystem)) return;
        dataList.Add(particleSystem);
        var main = particleSystem.main;
        Color color = GetEffect(player);
        main.startColor = new Color(color.r, color.g, color.b, main.startColor.color.a);
        _inverseAssignmentMap[particleSystem] = displayName;
    }

    public void AssignBehaviour(VRCPlayerApi player, UdonSharpBehaviour behaviour)
    {
        Remove(behaviour);
        string displayName = player.displayName;
        if (!_assignedBehaviours.ContainsKey(player.displayName))
        {
            _assignedBehaviours[displayName] = new DataList();
        }
        DataList dataList = _assignedBehaviours[displayName].DataList;
        
        if (dataList.Contains(behaviour)) return;
        dataList.Add(behaviour);
        behaviour.SendCustomEvent("ColorChanged");
        _inverseAssignmentMap[behaviour] = displayName;
    }

    public void Remove(Object assigned)
    {
        if (!_inverseAssignmentMap.ContainsKey(assigned)) return;
        string previousOwner = _inverseAssignmentMap[assigned].String;
        if (_assignedParticleSystems.ContainsKey(previousOwner)) _assignedParticleSystems[previousOwner].DataList.Remove(assigned);
        if (_assignedRenderers.ContainsKey(previousOwner)) _assignedRenderers[previousOwner].DataList.Remove(assigned);
        if (_assignedBehaviours.ContainsKey(previousOwner)) _assignedBehaviours[previousOwner].DataList.Remove(assigned);
    }
    
    public void ColorChanged(VRCPlayerApi player)
    {
        DataList log = new DataList();
        if (_assignedRenderers.ContainsKey(player.displayName))
        {
            DataList renderers = _assignedRenderers[player.displayName].DataList;
            log.Add($"{player.displayName} has {renderers.Count} renderers");
            MaterialPropertyBlock block = GetPropertyBlock(player);
            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i].IsNull) continue;
                Renderer renderer = (Renderer)renderers[i].Reference;
                renderer.SetPropertyBlock(block);
                log.Add($"Set propertyblock on {renderer.name}");
                log.Add(EventTracker.Instance().ConvertPropertyBlockToData(block));
            }
        }
        else
        {
            log.Add($"{player.displayName} has 0 renderers");
        }

        if (_assignedParticleSystems.ContainsKey(player.displayName))
        {
            DataList particleSystems = _assignedParticleSystems[player.displayName].DataList;
            log.Add($"{player.displayName} has {particleSystems.Count} particles");
            for (int i = 0; i < particleSystems.Count; i++)
            {
                if (particleSystems[i].IsNull) continue;
                ParticleSystem particleSystem = (ParticleSystem)particleSystems[i].Reference;
                var main = particleSystem.main;
                Color color = GetEffect(player);
                main.startColor = new Color(color.r, color.g, color.b, main.startColor.color.a);
                log.Add($"Set {particleSystem.name} color to {color}");
            }
        }
        else
        {
            log.Add($"{player.displayName} has 0 particles");
        }

        if (_assignedBehaviours.ContainsKey(player.displayName))
        {
            DataList behaviours = _assignedBehaviours[player.displayName].DataList;
            log.Add($"{player.displayName} has {behaviours.Count} behaviours");
            for (int i = 0; i < behaviours.Count; i++)
            {
                if (behaviours[i].IsNull) continue;
                UdonSharpBehaviour behaviour = (UdonSharpBehaviour)behaviours[i].Reference;
                behaviour.SendCustomEvent("ColorChanged");
                log.Add($"Sent ColorChanged Udon event to {behaviour.name}");
            }
        }
        else
        {
            log.Add($"{player.displayName} has 0 behaviours");
        }
        
        EventTracker.Instance().TrackEvent(nameof(ColorPicker), nameof(ColorChanged), gameObject)
            .AddParameter("Log", log);
    }

    public MaterialPropertyBlock GetPropertyBlock(VRCPlayerApi player)
    {
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        block.SetColor("_Color0", GetPrimary(player));
        block.SetColor("_Color1", GetSecondary(player));
        block.SetColor("_EmissionColor", GetEffect(player));
        return block;
    }
}