
using System;
using UdonSharp;
using UdonToolkit;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;
#if UNITY_EDITOR
[CustomEditor(typeof(AmbientZone))]
public class AmbientZoneEditor : UTEditor { }
#endif
public class AmbientZone : WorldPropTemplate
{
    public bool isGlobal;
    public float priority;

    public bool useProfile;
    [HideIf("@!useProfile")]
    public string profileName;

    [Toggle]
    [HideIf("@useProfile")]
    public bool controlFog;
    [HideIf("@useProfile")]
    [HideIf("@!controlFog")]
    public Color fogColor;
    [HideIf("@useProfile")]
    [HideIf("@!controlFog")]
    public float fogDensity;
    
    
    private AmbientManager _ambientManager;
    private bool _isActive;

    private void Activate()
    {
        if (_isActive) return;
        _isActive = true;
        if (!Utilities.IsValid(_ambientManager)) _ambientManager = AmbientManagerFinder.FindAmbientManager();
        _ambientManager.AddZone(this);
    }

    private void Deactivate()
    {
        if (!_isActive) return;
        _isActive = false;
        if (!Utilities.IsValid(_ambientManager)) _ambientManager = AmbientManagerFinder.FindAmbientManager();
        _ambientManager.RemoveZone(this);
    }
    
    private void OnEnable()
    {
        if (isGlobal) Activate();
    }

    private void OnDisable()
    {
        Deactivate();
    }

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (player.isLocal) Activate();
    }

    public override void OnPlayerTriggerExit(VRCPlayerApi player)
    {
        if (player.isLocal) Deactivate();
    }

    public override void DeserializeProp(DataDictionary parameters)
    {
        Debug.Log("ApplyParameters");
        if (!Utilities.IsValid(parameters)) return;
        VRCJson.TrySerializeToJson(parameters, JsonExportType.Beautify, out DataToken json);
        Debug.Log(json);
        currentParameters = parameters;
        if (currentParameters.TryGetValue("profileName", out DataToken token))
        {
            useProfile = true;
            profileName = token.String;
            controlFog = false;
            return;
        }

        if (currentParameters.TryGetValue("controlFog", TokenType.DataDictionary, out token))
        {
            Debug.Log("controlFog");
            DataDictionary fog = token.DataDictionary;
            controlFog = true;
            fogDensity = (float)fog["fogDensity"].Double;
            fogColor = TokenToColor(fog["fogColor"]);
        }
    }

    public override DataDictionary SerializeProp()
    {
        Debug.Log("GetParameters");
        if (!Utilities.IsValid(currentParameters)) currentParameters = new DataDictionary();
        currentParameters.Clear();
        if (useProfile)
        {
            currentParameters["profileName"] = profileName;
            return currentParameters;
        }

        if (controlFog)
        {
            currentParameters["controlFog"] = new DataDictionary();
            DataDictionary dictionary = currentParameters["controlFog"].DataDictionary;
            dictionary["fogDensity"] = fogDensity;
            dictionary["fogColor"] = ColorToToken(fogColor);
        }

        return currentParameters;
    }


    private Color TokenToColor(DataToken token)
    {
        Color color = new Color();
        color.r = (float)token.DataList[0].Number;
        color.g = (float)token.DataList[1].Number;
        color.b = (float)token.DataList[2].Number;
        color.a = (float)token.DataList[3].Number;
        return color;
    }

    private DataToken ColorToToken(Color color)
    {
        DataList list = new DataList();
        list.Add(color.r);
        list.Add(color.g);
        list.Add(color.b);
        list.Add(color.a);
        return list;
    }


    [Button("Editor Preview")]
    public void EditorPreview()
    {
        RenderSettings.fog = controlFog;
        if (controlFog)
        {
            RenderSettings.fog = true;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity;
        }
    }
}
