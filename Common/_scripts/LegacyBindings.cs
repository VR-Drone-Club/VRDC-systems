
using System;
using Phasedragon.AdminUtilities;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDK3.Persistence;
using VRC.SDK3.Rendering;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

public class LegacyBindings : UdonSharpBehaviour
{
    public ParticleSystem ParticleSystem;
    private DataDictionary _bindings = new DataDictionary();
    private DataDictionary _runtimeData = new DataDictionary();

    [NonSerialized]
    public string key;
    [NonSerialized]
    public float min;
    [NonSerialized]
    public float max;
    [NonSerialized]
    public string source;
    
    
    private string[] availableSources = new string[]
    {
        "Oculus_GearVR_RThumbstickY",
        "Oculus_GearVR_RThumbstickX",
        "Oculus_GearVR_LThumbstickX",
        "Oculus_GearVR_LThumbstickY",
        "Fire1",
        "Joy1 Axis 1",
        "Joy1 Axis 2",
        "Joy1 Axis 3",
        "Joy1 Axis 4",
        "Joy1 Axis 5",
        "Joy1 Axis 6",
        "Joy1 Axis 7",
        "Joy1 Axis 8",
    };
    void Start()
    {
        RegisterEvent("Test", this, nameof(Test));
    }
    
    public void Test()
    {
        Debug.Log("Test successful!");
        SendCustomNetworkEvent(NetworkEventTarget.All, nameof(FireParticles), VRCCameraSettings.PhotoCamera.Position, VRCCameraSettings.PhotoCamera.Forward);
    }
    [NetworkCallable]
    public void FireParticles(Vector3 pos, Vector3 dir)
    {
        ParticleSystem.transform.position = pos;
        ParticleSystem.transform.LookAt(pos + dir);
        ParticleSystem.Play();
    }
    private void Update()
    {
        DataList keys = _bindings.GetKeys();
        for (int i = 0; i < keys.Count; i++)
        {
            if (!_runtimeData.ContainsKey(keys[i])) continue;
            DataDictionary bindingEntry = _bindings[keys[i]].DataDictionary;
            if (bindingEntry["target"].IsNull) continue;
            string source = bindingEntry["source"].String;
            float min = (float)bindingEntry["min"].Double;
            float max = (float)bindingEntry["max"].Double;
            if (string.IsNullOrEmpty(source)) continue;
            float value = Input.GetAxisRaw(source);
            bool state = (value >= min && value <= max);
            DataDictionary runtimeEntry = _runtimeData[keys[i]].DataDictionary;
            Debug.Log($"Testing '{source}': '{value}' between {min} and {max}. Current: {state} Previous {runtimeEntry["state"]}");
            if (runtimeEntry["state"].Boolean == state)
            {
                continue;
            }
            runtimeEntry["state"] = state;
            if (!state)
            {
                continue;
            }
            //Debug.Log($"Executing event {runtimeEntry["event"]}");
            UdonSharpBehaviour target = (UdonSharpBehaviour)runtimeEntry["target"].Reference;
            target.SendCustomEvent(runtimeEntry["event"].String);
        }
    }

    public void RegisterEvent(string bindingName, UdonSharpBehaviour target, string eventName)
    {

        DataDictionary runtimeEntry = new DataDictionary();
        _runtimeData[bindingName] = runtimeEntry;
        runtimeEntry["state"] = false;
        runtimeEntry["target"] = target;
        runtimeEntry["event"] = eventName;
        if (!_bindings.ContainsKey(bindingName))
        {
            DataDictionary bindingEntry = new DataDictionary();
            _bindings[bindingName] = bindingEntry;
            bindingEntry["min"] = 0.1f;
            bindingEntry["max"] = 1.1f;
            bindingEntry["source"] = "Fire1";
        }

        QuickMenu.Instance().RegisterFloat($"Bindings/{bindingName}/min", this, nameof(min), nameof(SetMin), -1.1f, 1.1f)
            .WithPropertyAdditionalVariable(nameof(key), bindingName)
            .WithPropertyCustomGetter(nameof(key), bindingName, nameof(GetMin));
        QuickMenu.Instance().RegisterFloat($"Bindings/{bindingName}/max", this, nameof(max), nameof(SetMax), -1.1f, 1.1f)
            .WithPropertyAdditionalVariable(nameof(key), bindingName)
            .WithPropertyCustomGetter(nameof(key), bindingName, nameof(GetMax));
        QuickMenu.Instance().RegisterStringEditor($"Bindings/{bindingName}/source", this, nameof(source), nameof(SetSource))
            .WithPropertyAdditionalVariable(nameof(key), bindingName)
            .WithPropertyCustomGetter(nameof(key), bindingName, nameof(GetSource));
        
        PackData();
    }

    public void GetMin()
    {
        min = (float)_bindings[key].DataDictionary["min"].Double;
    }
    public void SetMin()
    {
        _bindings[key].DataDictionary["min"] = min;
        PackData();
    }
    public void GetMax()
    {
        max = (float)_bindings[key].DataDictionary["max"].Double;
    }
    public void SetMax()
    {
        _bindings[key].DataDictionary["max"] = max;
        PackData();
    }
    public void GetSource()
    {
        source = _bindings[key].DataDictionary["source"].String;
    }
    public void SetSource()
    {
        _bindings[key].DataDictionary["source"] = source;
        PackData();
    }

    public void PackData()
    {
        if (!VRCJson.TrySerializeToJson(_bindings, JsonExportType.Minify, out DataToken result))
        {
            Debug.LogError($"Failed to pack data {result}");
            return;
        }
        PlayerData.SetString("LegacyBindings", result.String);
        Debug.Log($"Set LegacyBindings to {result.String}");
    }

    public override void OnPlayerRestored(VRCPlayerApi player)
    {
        if (!player.isLocal) return;
        if (PlayerData.TryGetString(Networking.LocalPlayer, "LegacyBindings", out string result))
        {
            Debug.LogError("Data did not exist");
            return;
        }

        if (!VRCJson.TryDeserializeFromJson(result, out var token))
        {
            Debug.LogError($"Failed to parse data {token}");
            return;
        }

        _bindings = token.DataDictionary;
    }
}
