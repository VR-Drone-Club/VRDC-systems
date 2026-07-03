
using System;
using System.Text;
using System.Text.RegularExpressions;
using Phasedragon.AdminUtilities;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDK3.Data;
using VRC.SDK3.StringLoading;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;
using VRDC_systems.Building.Scripts.Building;

public class BuildManager : UdonSharpBehaviour
{
    public Transform templateParent;
    public Builder[] builders;
    public MenuBarRegistry menuBarRegistry;

    private ZoneManager _zoneManager;
    private DataDictionary _templates;
    public DataDictionary PropPools => _propPools;
    private DataDictionary _propPools = new DataDictionary();
    private DataDictionary _propPoolCounts = new DataDictionary();
    private bool _initialized;
    private DataDictionary _propsByUUID = new DataDictionary();
    private DataDictionary _delayedParameters = new DataDictionary();
    private string _appliedSave;


    public static BuildManager Instance()
    {
        GameObject buildManagerObject = GameObject.Find("BuildManager");
        return buildManagerObject.GetComponent<BuildManager>();
    }
    void Start()
    {
        Initialize();
        foreach (var builder in builders)
        {
            builder.Initialize();
        }
        menuBarRegistry.RegisterMenuItem("File/Export", this, nameof(Export));
    }

    public void Export()
    {
        Debug.Log(ExportSave());
    }
    
    public void Initialize()
    {
        if (_initialized) return;
        _zoneManager = ZoneManager.Instance();
        _blueprints = new DataDictionary();
        _currentBlueprintSelection = new DataList();
        _templates = new DataDictionary();
        for (int i = 0; i < templateParent.childCount; i++)
        {
            if (!Utilities.IsValid(templateParent.GetChild(i))) continue;
            var template = templateParent.GetChild(i);
            _templates.Add(template.name, template);
            _propPools[template.name] = new DataList();
            _propPoolCounts[template.name] = 0;
        }
        templateParent.gameObject.SetActive(false);
        _initialized = true;
        GatherExistingProps();
    }
    private void GatherExistingProps()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
#if UNITY_EDITOR && !COMPILER_UDONSHARP
            string foundName = Regex.Replace(child.name, "\\s\\(\\d+\\)$", "");
            child.name = foundName;
#else
            string foundName = child.name;
#endif
            if (!_templates.ContainsKey(foundName))
            {
                Debug.LogError($"Found object '{foundName}' not a recognized template");
                continue;
            }
            
            _propPools[foundName].DataList.Add(child.gameObject);
            child.gameObject.SetActive(false);
        }
    }
    
    public string ExportSave()
    {
        DataDictionary save = new DataDictionary();
        
        DataList keys = _propPools.GetKeys();
        for (int i = 0; i < keys.Count; i++)
        {
            DataList pool = _propPools[keys[i]].DataList;
            int count = _propPoolCounts[keys[i]].Int;
            
            //Debug.Log($"Exporting {keys[i]}: {pool.Count}");
            if (pool.Count < 1 || count < 1) continue;
            
            DataList exportedPool = new DataList();
            save[keys[i]] = exportedPool;
            
            for (int j = 0; j < pool.Count; j++)
            {
                if (j >= count) break;
                GameObject prop = (GameObject)pool[j].Reference;
                DataList propProperties = new DataList();
                propProperties.Add(PositionToToken(prop.transform.position));
                propProperties.Add(RotationToToken(prop.transform.rotation));
                WorldPropTemplate propTemplate = prop.GetComponent<WorldPropTemplate>();
                if (Utilities.IsValid(propTemplate) && Utilities.IsValid(propTemplate.currentParameters))
                {
                    propTemplate.BuildManager = this;
                    propProperties.Add(propTemplate.currentParameters);
                    if (propTemplate.currentParameters.ContainsKey("uuid"))
                    {
                        _propsByUUID[propTemplate.currentParameters["uuid"]] = propTemplate;
                    }
                }
                exportedPool.Add(propProperties);
            }
        }

        VRCJson.TrySerializeToJson(save, JsonExportType.Minify, out DataToken result);
        //Debug.Log(result.ToString());
        return result.ToString();
    }

    public void LoadSave(string save)
    {
        if (!_initialized) Initialize();
        if (_appliedSave == save) return;
        _appliedSave = save;
        
        Clear();
        
        if (!VRCJson.TryDeserializeFromJson(save, out DataToken token) || token.TokenType != TokenType.DataDictionary)
        {
            Debug.LogError($"Unable to load save: {token.ToString()}");
            return;
        }

        DataDictionary dictionary = token.DataDictionary;
        DataList templates = dictionary.GetKeys();
        for (int i = 0; i < templates.Count; i++)
        {
            string template = templates[i].String;
            DataList props = dictionary[template].DataList;
            for (int j = 0; j < props.Count; j++)
            {
                DataList prop = props[j].DataList;
                ExtractPropInfo(prop, out Vector3 position, out Quaternion rotation, out DataDictionary parameters);
                SpawnPropInternal(template, position, rotation, parameters);
            }
        }
        ApplyParameters();
        DisableUnusedProps();
    }

    internal void ExtractPropInfo(DataList prop, out Vector3 position, out Quaternion rotation, out DataDictionary parameters)
    {
        position = TokenToPosition(prop[0]); 
        rotation = TokenToRotation(prop[1]); 
        parameters = prop.Count >= 3 ? prop[2].DataDictionary : null;
    }

    public Transform GetPropPreview(string name)
    {
        Transform template = GetTemplate(name);
        if (!Utilities.IsValid(template)) return null;
        GameObject newProp = Instantiate(template.gameObject);
        
        return newProp.transform;
    }
    public GameObject SpawnPropSynced(string name, Vector3 position, Quaternion rotation)
    {
        return SpawnPropSynced(name, position, rotation, null);
    }
    public GameObject SpawnPropSynced(string name, Vector3 position, Quaternion rotation, DataDictionary parameters)
    {
        GameObject prop = SpawnPropInternal(name, position, rotation, parameters);
        ApplyParameters();
        _zoneManager.BuildManagerChanged();
        return prop;
    }
    
    //PropPools
        //Key(string name) = Value(DataList pool)
            //GameObject prop

    private GameObject SpawnPropInternal(string name, Vector3 position, Quaternion rotation, DataDictionary parameters)
    {
        if (parameters != null && parameters.ContainsKey("uuid") && _propsByUUID.ContainsKey(parameters["uuid"]))
        {
            Debug.LogError($"Unable to spawn prop: UUID {parameters["uuid"]} already exists");
            return null;
        }
        if (!_templates.ContainsKey(name))
        {
            Debug.LogError($"Unable to spawn prop: Template '{name}' not found");
            return null;
        }
        position = RoundPosition(position);
        rotation = RoundRotation(rotation);
        DataList pool = _propPools[name].DataList;
        int count = _propPoolCounts[name].Int;
        if (pool.TryGetValue(count, out DataToken potentialProp)) 
        {
            //Retrieve existing prop
            GameObject newProp = (GameObject)potentialProp.Reference;
            newProp.SetActive(true);
            newProp.transform.SetPositionAndRotation(position, rotation);
            WorldPropTemplate propTemplate = newProp.GetComponent<WorldPropTemplate>();
            if (Utilities.IsValid(propTemplate))
            {
                propTemplate.BuildManager = this;
                DelayParameters(propTemplate, parameters);
                
                if (parameters != null && parameters.ContainsKey("uuid"))
                {
                    _propsByUUID[parameters["uuid"]] = propTemplate;
                }
            }
            _propPoolCounts[name] = count + 1;
            return newProp;
        }
        else //If TryGetValue fails, that means we need to instantiate new props to expand the pool
        {
            //Instantiate new prop
            Transform template = GetTemplate(name);
            GameObject newProp = Instantiate(template.gameObject);
            newProp.transform.SetPositionAndRotation(position, rotation);
            newProp.transform.SetParent(transform);
            newProp.name = name;
            WorldPropTemplate propTemplate = newProp.GetComponent<WorldPropTemplate>();
            if (Utilities.IsValid(propTemplate))
            {
                propTemplate.BuildManager = this;
                DelayParameters(propTemplate, parameters);
                if (parameters != null && parameters.ContainsKey("uuid"))
                {
                    _propsByUUID[parameters["uuid"]] = propTemplate;
                }
            }
            pool.Add(newProp);
            _propPoolCounts[name] = count + 1;
            
            return newProp;
        }
    }

    private void DelayParameters(WorldPropTemplate prop, DataDictionary parameters)
    {
        _delayedParameters[prop] = parameters;
    }
    // we need to ensure that parameters are applied after all props have been spawned, in case they cross reference eachother
    private void ApplyParameters()
    {
        if (_delayedParameters.Count == 0) return;
        DataList props = _delayedParameters.GetKeys();
        for (int i = 0; i < props.Count; i++)
        {
            if (props[i].IsNull) continue;
            WorldPropTemplate propTemplate = (WorldPropTemplate)props[i].Reference;
            if (_delayedParameters.TryGetDataDictionary(propTemplate, out DataDictionary parameters))
            {
                propTemplate.ApplyParameters(parameters);
            }
        }
        _delayedParameters.Clear();
    }
    public void ReturnPropSynced(GameObject prop)
    {
        ReturnProp(prop);
        _zoneManager.BuildManagerChanged();
    }

    public void PositionsDirty()
    {
        _zoneManager.BuildManagerChanged();
        // this manages syncing zone data after it's been changed
    }
    public bool IsRegisteredProp(GameObject prop)
    {
        if (!Utilities.IsValid(prop)) return false;
        if (!_propPools.ContainsKey(prop.name)) // early name check
        {
            return false;
        }
        DataList pool = _propPools[prop.name].DataList; // more complete pool check
        int index = pool.IndexOf(prop);
        if (index == -1)
        {
            return false;
        }
        return true;
    }

    public WorldPropTemplate GetPropByUUID(string uuid)
    {
        if (_propsByUUID.ContainsKey(uuid))
        {
            return (WorldPropTemplate)_propsByUUID[uuid].Reference;
        }

        return null;
    }
    private void ReturnProp(GameObject prop)
    {
        if (!Utilities.IsValid(prop)) return;
        if (!_propPools.ContainsKey(prop.name))
        {
            Debug.LogError($"Unable to return object '{prop.name}' as it's not a recognized prop");
            return;
        }
        DataList pool = _propPools[prop.name].DataList;
        int count = _propPoolCounts[prop.name].Int;
        int index = pool.IndexOf(prop);
        if (index == -1)
        {
            Debug.LogError($"Unable to return object '{prop.name}' as it's not a recognized prop");
            return;
        }
        if (index >= count)
        {
            Debug.LogError($"Unable to return object '{prop.name}' as it's already returned");
            return;
        }

        WorldPropTemplate propTemplate = prop.GetComponent<WorldPropTemplate>();
        if (Utilities.IsValid(propTemplate))
        {
            if (Utilities.IsValid(propTemplate.currentParameters) && propTemplate.currentParameters.ContainsKey("uuid"))
            {
                _propsByUUID.Remove(propTemplate.currentParameters["uuid"]);
            }
        }
        
        pool.RemoveAt(index); //Remove from pool at current index
        _propPoolCounts[prop.name] = count - 1; //Decrement pool count
        pool.Add(prop); //Add back at end of pool so it can be retrieved later
        prop.SetActive(false);
    }

    public void Clear()
    {
        ResetPools();
        DisableUnusedProps();
    }
    private void ResetPools()
    {
        var keys = _propPoolCounts.GetKeys();
        for (int i = 0; i < keys.Count; i++)
        {
            _propPoolCounts[keys[i]] = 0;
        }
        _propsByUUID.Clear();
    }

    private void DisableUnusedProps()
    {
        var keys = _propPoolCounts.GetKeys();
        for (int i = 0; i < keys.Count; i++)
        {
            DataList pool = _propPools[keys[i]].DataList;
            int count = _propPoolCounts[keys[i]].Int;
            if (pool.Count == count) continue;
            for (int j = count; j < pool.Count; j++)
            {
                GameObject prop = (GameObject)pool[j].Reference;
                prop.SetActive(false);
            }
        }
    }

    private DataDictionary _blueprints;
    private DataList _currentBlueprintSelection;

    private void AddToBlueprintSelection()
    {
        
    }

    private void RemoveFromBlueprintSelection()
    {
        
    }

    private void SaveBlueprint()
    {
        
    }

    public DataList GetTemplates()
    {
        Initialize();
        return _templates.GetKeys();
    }
    
    public Transform GetTemplate(string name)
    {
        if (!_templates.ContainsKey(name)) return null;
        return (Transform)_templates[name].Reference;
    }

    private Vector3 TokenToPosition(DataToken token)
    {
        DataList list = token.DataList;
        if (doRounding)
        {
            return new Vector3((float)(list[0].Number / positionRounding), (float)(list[1].Number / positionRounding), (float)(list[2].Number / positionRounding));
        }
        else
        {
            return new Vector3((float)(list[0].Number), (float)(list[1].Number), (float)(list[2].Number));
        }
    }

    public DataToken PositionToToken(Vector3 vector)
    {
        DataList list = new DataList();
        if (doRounding)
        {
            list.Add(Mathf.RoundToInt(vector.x * positionRounding));
            list.Add(Mathf.RoundToInt(vector.y * positionRounding));
            list.Add(Mathf.RoundToInt(vector.z * positionRounding));
        }
        else
        {
            list.Add(vector.x);
            list.Add(vector.y);
            list.Add(vector.z);
        }
        return list;
    }
    
    
    public Quaternion TokenToRotation(DataToken token)
    {
        if (token.TokenType == TokenType.DataList)
        {
            DataList list = token.DataList;
            return Quaternion.Euler(new Vector3((float)list[0].Number, (float)list[1].Number, (float)list[2].Number));
        }
        else if (token.IsNumber)
        {
            return Quaternion.Euler(new Vector3(0, (float)token.Number, 0));
        }
        return Quaternion.identity;
    }

    public DataToken RotationToToken(Quaternion rotation)
    {
        Vector3 vector = rotation.eulerAngles;
        if (Mathf.Approximately(vector.x, 0) && Mathf.Approximately(vector.z, 0))
        {
            return vector.y;
        }
        DataList list = new DataList();
        list.Add(vector.x);
        list.Add(vector.y);
        list.Add(vector.z);
        return list;
    }

    public bool doRounding = true;
    public float positionRounding = 5;
    public float rotationRounding = 90;
    public Vector3 RoundPosition(Vector3 position)
    {
        if (!doRounding) return position;
        position = position * positionRounding;
        position.x = Mathf.RoundToInt(position.x);
        position.y = Mathf.RoundToInt(position.y);
        position.z = Mathf.RoundToInt(position.z);
        position = position / positionRounding;
        return position;
    }

    public Quaternion RoundRotation(Quaternion rotation)
    {
        if (!doRounding) return rotation;
        Vector3 eulerAngles = rotation.eulerAngles;
        eulerAngles = eulerAngles / rotationRounding;
        eulerAngles.x = Mathf.RoundToInt(eulerAngles.x);
        eulerAngles.y = Mathf.RoundToInt(eulerAngles.y);
        eulerAngles.z = Mathf.RoundToInt(eulerAngles.z);
        eulerAngles = eulerAngles * rotationRounding;
        return Quaternion.Euler(eulerAngles);
    }

    public bool IsValid(Vector3 vector3)
    {
        if (Single.IsNaN(vector3.x)) return false;
        if (Single.IsNaN(vector3.y)) return false;
        if (Single.IsNaN(vector3.z)) return false;
        return true;
    }
}
