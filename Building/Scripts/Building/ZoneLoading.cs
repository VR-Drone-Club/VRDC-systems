
using System;
using System.Collections;
using System.IO;
using Phasedragon.AdminUtilities;
using UdonSharp;
using UdonToolkit;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

#if UNITY_EDITOR
[CustomEditor(typeof(ZoneLoading))]
public class ZoneLoadingEditor : UTEditor { }
#endif
public static class ZoneLoadingFinder
{
    public static ZoneLoading FindZoneLoading()
    {
        GameObject gameObject = GameObject.Find("ZoneLoading");
        if (!Utilities.IsValid(gameObject)) return null;
        return gameObject.GetComponent<ZoneLoading>();
    }
}
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class ZoneLoading : GenericPool
{
    public string defaultZone;
    public TextAsset worldMap;
    [ListView("Zones")]
    public TextAsset[] zoneFiles;
    
    [UdonSynced]
    private string _syncedZone;
    [UdonSynced] 
    private string _targetPortal;
    
    private ActorPool _actorPool;
    private UdonShellCore _udonShellCore;

    internal override void Start()
    {
        if (Networking.IsMaster) LoadWorldData(defaultZone, String.Empty);

        //quickMenu.RegisterInt("Zones/Set zone", this, nameof(defaultZone), nameof(SetZone), 0, zoneFiles.Length - 1);
        /*
        for (int i = 0; i < zoneFiles.Length; i++)
        {
            quickMenu.RegisterEvent($"Zones/{(i < zoneNames.Length ? zoneNames[i] : i.ToString())}", this, nameof(SetZone)).WithPropertyAdditionalVariable(nameof(defaultZone), i);
        }*/
        
        #if UDONSHELL
        UdonShellReferenceManager referenceManager = UdonShellReferenceManager.Instance();
        if (Utilities.IsValid(referenceManager))
        {
            _udonShellCore = referenceManager.UdonShellCore;
        }

        if (Utilities.IsValid(_udonShellCore))
        {
            _udonShellCore.RegisterFunction(this, nameof(LoadZone), "Utilities")
                .WithArgument(nameof(worldDataInput), "string").WithOverflow();
        }
        
        #endif
    }

    internal override void Initialize()
    {
        base.Initialize();
        QuickMenu quickMenu = QuickMenu.Instance();
        _actorPool = ActorPoolFinder.FindActorPool();
    }

    private DataList _eventListeners = new DataList();
    public void RegisterEventListener(Component component)
    {
        _eventListeners.Add(component);
        if (_loaded)
        {
            UdonBehaviour behaviour = (UdonBehaviour)component;
            behaviour.SendCustomEvent("SaveLoaded");
        }
    }

    private void SendEventToListeners()
    {
        for (int i = 0; i < _eventListeners.Count; i++)
        {
            while (_eventListeners[i].IsNull)
            {
                _eventListeners.RemoveAt(i);
                if (i >= _eventListeners.Count) break;
            }
            UdonBehaviour behaviour = (UdonBehaviour)_eventListeners[i].Reference;
            behaviour.SendCustomEvent("SaveLoaded");
        }
    }

    public void SetZone()
    {
        LoadWorldData(defaultZone, string.Empty);
    }
    [Button("Save")]
    public void SaveZone()
    {
        Initialize();
        string save = ExportSave();
        SetZoneFile(defaultZone, save);
    }

    public void LinkNewPortal(ZonePortal portal, string targetZone, string targetPortal)
    {
        if (ZoneExists(targetZone))
        {
            CreateNewPortal(portal, targetZone, targetPortal);
        }
        else
        {
            CreateNewZone(portal, targetZone, targetPortal);
        }
    }

    private void CreateNewPortal(ZonePortal portal, string targetZone, string targetPortal)
    {
        string json = GetZoneFile(targetZone);
        if (!VRCJson.TryDeserializeFromJson(json, out DataToken existingToken))
        {
            Debug.LogError($"Failed to deserialize zone: {existingToken}");
            return;
        }

        DataDictionary dictionary = existingToken.DataDictionary;
        DataList list = dictionary.ContainsKey("ZonePortal") ? dictionary["ZonePortal"].DataList : new DataList();
        
        DataList zonePortal = new DataList();
        zonePortal.Add(PositionToToken(RoundPosition(portal.transform.localRotation * new Vector3(0,0,-4000))));
        zonePortal.Add(RotationToToken(RoundRotation(portal.transform.localRotation* Quaternion.Euler(0, 180, 0))));
        
        DataDictionary parameters = new DataDictionary();
        parameters["targetZone"] = GetCurrentZoneName();
        parameters["targetPortal"] = portal.thisPortal;
        parameters["thisPortal"] = targetPortal;

        zonePortal.Add(parameters);
        
        list.Add(zonePortal);
        dictionary["ZonePortal"] = list;

        VRCJson.TrySerializeToJson(dictionary, JsonExportType.Minify, out DataToken result);
        SetZoneFile(targetZone, result.String);
    }
    private void CreateNewZone(ZonePortal portal, string targetZone, string targetPortal)
    {
        DataDictionary dictionary = new DataDictionary();
        
        DataList list = new DataList();
        dictionary["ZonePortal"] = list;
        
        DataList zonePortal = new DataList();
        list.Add(zonePortal);
        
        zonePortal.Add(PositionToToken(RoundPosition(portal.transform.localPosition * -1)));
        zonePortal.Add(RotationToToken(RoundRotation(portal.transform.localRotation) * Quaternion.Euler(0, 180, 0)));
        DataDictionary parameters = new DataDictionary();
        parameters["targetZone"] = GetCurrentZoneName();
        parameters["targetPortal"] = portal.thisPortal;
        parameters["thisPortal"] = targetPortal;
        zonePortal.Add(parameters);

        VRCJson.TrySerializeToJson(dictionary, JsonExportType.Minify, out DataToken result);
        SetZoneFile(targetZone, result.String);
    }
    
    [Button("Reload zone")]
    public void ReloadZoneEditor()
    {
        LoadZoneEditor(defaultZone);
    }

    [Button("Load world map")]
    public void LoadWorldMap()
    {
        if (!Utilities.IsValid(worldMap))
        {
            Debug.LogError("WorldMap has not been set up");
            return;
        }
        Initialize();
        defaultZone = worldMap.name;
        LoadSave(worldMap.text);
    }
    
    public bool LoadZoneEditor(string zone)
    {
        if (!ZoneExists(zone))
        {
            Debug.LogError($"Zone '{zone}' does not exist");
            return false;
        }
        Initialize();
        defaultZone = zone;
        LoadSave(GetZoneFile(zone));
        return true;
    }

    public bool ZoneExists(string zoneName)
    {
        for (int i = 0; i < zoneFiles.Length; i++)
        {
            if (!Utilities.IsValid(zoneFiles[i])) continue;
            if (zoneFiles[i].name == zoneName) return true;
        }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
        var assets = AssetDatabase.FindAssets("t:TextAsset", new string[] { "Assets/Zones"});
        zoneFiles = new TextAsset[assets.Length];
        for (int i = 0; i < assets.Length; i++)
        {
            zoneFiles[i] = AssetDatabase.LoadAssetAtPath<TextAsset>(AssetDatabase.GUIDToAssetPath(assets[i]));
            assets[i] = AssetDatabase.GUIDToAssetPath(assets[i]).Replace("Assets/Zones/", string.Empty).Replace(".json", string.Empty);
        }
        
        for (int i = 0; i < zoneFiles.Length; i++)
        {
            if (!Utilities.IsValid(zoneFiles[i])) continue;
            if (zoneFiles[i].name == zoneName) return true;
        }
#endif
        return false;
    }
    public string GetZoneFile(string zoneName)
    {
        foreach (var zonefile in zoneFiles)
        {
            if (!Utilities.IsValid(zonefile)) continue;
            if (zonefile.name == zoneName) return zonefile.text;
        }
        
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        string path = Application.dataPath + $"/Zones/{zoneName}.json";
        return File.ReadAllText(path);
#endif
        Debug.LogError($"Unable to find zone named {zoneName}");
        return string.Empty;
    }

    public void SetZoneFile(string zoneName, string zoneFile)
    {
        #if UNITY_EDITOR && !COMPILER_UDONSHARP
        TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>($"Assets/Zones/{zoneName}.json");
        if (!Utilities.IsValid(asset))
        {
            TextAsset newAsset = new TextAsset();
            AssetDatabase.CreateAsset(newAsset, $"Assets/Zones/{zoneName}.json");
        }
        string path = Application.dataPath + $"/Zones/{zoneName}.json";
        Debug.Log($"Writing all text to {path}");
        File.WriteAllText(path, zoneFile);
        AssetDatabase.ImportAsset($"Assets/Zones/{zoneName}.json");
        #endif
    }
    
    
    [Button("Export Save")]
    public void ExportZoneEditor()
    {
        Initialize();
        ExportSave();
    }

    [NonSerialized] 
    public string worldDataInput;
    public void LoadZone()
    {
        string zone = GetZoneFile(worldDataInput);
        if (string.IsNullOrEmpty(zone)) zone = worldDataInput;
        if (!LoadSave(zone)) return;
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        _syncedZone = zone;
        RequestSerialization();
        SendEventToListeners();
    }
    public void LoadWorldData(string zone, string portal)
    {
        if (!_initialized) Initialize();
        if (Utilities.IsValid(_actorPool)) _actorPool.ClearActors();
        Debug.Log($"Loading zone {zone} to portal {portal}");
        if (!LoadSave(GetZoneFile(zone))) return;
        Debug.Log("Zone loading succeeded");
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        _syncedZone = zone;
        _targetPortal = portal;
        RequestSerialization();
        SendEventToListeners();
        if (!string.IsNullOrEmpty(_targetPortal)) EnterPortal();
    }

    public override void OnDeserialization()
    {
        if (!_initialized) Initialize();
        if (Utilities.IsValid(_actorPool)) _actorPool.ClearActors();
        LoadSave(GetZoneFile(_syncedZone));
        SendEventToListeners();
        
        if (!string.IsNullOrEmpty(_targetPortal)) EnterPortal();
    }

    public void EnterPortal()
    {
        ZonePortal portal = GetPortal(_targetPortal);
        if (!Utilities.IsValid(portal)) return;
        Transform entry = portal.entryPoint;
        // TODO: Stuff here to teleport
    }

    public ZonePortal GetPortal(string portalName)
    {
        var pool = _activePools["ZonePortal"].DataList;
        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i].IsNull) continue;
            GameObject obj = (GameObject)pool[i].Reference;
            if (!Utilities.IsValid(obj)) { Debug.Log("object was not valid"); continue; }
            ZonePortal portal = obj.GetComponentInChildren<ZonePortal>();
            if (!Utilities.IsValid(portal)) { Debug.Log("object was not valid"); continue; }
            if (portal.thisPortal != portalName) continue;
            Debug.Log($"Found portal {portal.thisPortal}");
            return portal;
        }
        Debug.LogError($"Was unable to find portal named '{portalName}' in zone '{GetCurrentZoneName()}'");
        return null;
    }


    public string GetCurrentZoneFile()
    {
        return GetZoneFile(GetCurrentZoneName());
    }
    public string GetCurrentZoneName()
    {
        #if UNITY_EDITOR && !COMPILER_UDONSHARP
        return Application.isPlaying ? _syncedZone : defaultZone;
#else
        return _syncedZone;
#endif
    }

    private bool hasDrawn;
    #if UNITY_EDITOR && !COMPILER_UDONSHARP
    private void OnDrawGizmos()
    {
        if (hasDrawn) return;
        if (Application.isPlaying) return;
        hasDrawn = true;
        try
        {
            ExportSave();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        Debug.Log("Reloading existing zone to clear out potential garbage. If this clears wanted changes, the backup export can be retrieved above");
        ReloadZoneEditor();
    }
    #endif 
}
