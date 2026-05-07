#if UNITY_EDITOR && !COMPILER_UDONSHARP
using System.IO;
using System.Text.RegularExpressions;
#endif

using System;
using System.Diagnostics;
using TMPro;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

public class GenericPool : UdonSharpBehaviour
{
    public Transform templateParent;
    public bool doRounding;
    public float positionRounding = 5;
    public float rotationRounding = 90;
    internal DataDictionary templates;
    internal DataDictionary _activePools;
    internal DataDictionary _inactivePools;
    internal bool _initialized;
    internal bool _loaded;
    internal virtual void Start()
    {
        Initialize();
    }
    internal virtual void Initialize()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        templates = new DataDictionary();
        _activePools = new DataDictionary();
        _inactivePools = new DataDictionary();
        for (int i = 0; i < templateParent.childCount; i++)
        {
            if (!Utilities.IsValid(templateParent.GetChild(i))) continue;
            var template = templateParent.GetChild(i);
            templates.Add(template.name.ToLower(), template);
            _activePools[template.name.ToLower()] = new DataList();
            _inactivePools[template.name.ToLower()] = new DataList();
        }
        templateParent.gameObject.SetActive(false);
        _initialized = true;
        GatherExistingProps();
    }
    private void GatherExistingProps()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            #if UNITY_EDITOR && !COMPILER_UDONSHARP
            string foundName = Regex.Replace(child.name.ToLower(), "\\s\\(\\d+\\)$", "");
            child.name = foundName;
            #else
            string foundName = child.name;
            #endif
            if (!templates.ContainsKey(foundName))
            {
                Debug.LogError($"Found object '{foundName}' not a recognized template");
                continue;
            }

            if (child.gameObject.activeSelf)
            {
                _activePools[foundName].DataList.Add(child.gameObject);
            }
            else
            {
                _inactivePools[foundName].DataList.Add(child.gameObject);
            }
        }
    }
    
    public string ExportSave()
    {
        if (!_initialized) Initialize();
        
        DataDictionary save = new DataDictionary();
        
        DataList keys = _activePools.GetKeys();
        keys.Sort();
        for (int i = 0; i < keys.Count; i++)
        {
            DataList pool = _activePools.GetList(keys[i]);
            if (pool.Count < 1) continue;
            SortByPosition(pool);
            
            DataList exportedPool = new DataList();
            save[keys[i]] = exportedPool;
            
            for (int j = 0; j < pool.Count; j++)
            {
                if (pool[j].IsNull) continue;
                GameObject prop = (GameObject)pool[j].Reference;
                if (!prop.activeInHierarchy) continue;
                DataList propProperties = new DataList();
                propProperties.Add(PositionToToken(prop.transform.localPosition));
                propProperties.Add(RotationToToken(prop.transform.localRotation));
                WorldPropTemplate propTemplate = prop.GetComponentInChildren<WorldPropTemplate>();
                if (Utilities.IsValid(propTemplate))
                {
                    DataDictionary parameters = propTemplate.GetParameters();
                    if (Utilities.IsValid(parameters)) propProperties.Add(parameters);
                    DataList spriteData = propTemplate.GetSpriteData();
                    if (Utilities.IsValid(spriteData)) propProperties.Add(spriteData);
                }
                exportedPool.Add(propProperties);
            }
        }

        VRCJson.TrySerializeToJson(save, JsonExportType.Minify, out DataToken result);
        Debug.Log(result.ToString());
        return result.ToString();
    }

    private void SortByPosition(DataList objectList)
    {
        var n = objectList.Count;
        for (int i = 0; i < n - 1; i++)
        {
            for (int j = 0; j < n - i - 1; j++)
            {
                if (CompareObjects(objectList[j], objectList[j + 1]))
                {
                    var tempVar = objectList[j];
                    objectList[j] = objectList[j + 1];
                    objectList[j + 1] = tempVar;
                }
            }
        }
    }

    private bool CompareObjects(DataToken a, DataToken b)
    {
        GameObject objectA = (GameObject)a.Reference;
        GameObject objectB = (GameObject)b.Reference;
        return (objectA.transform.localPosition.z > objectB.transform.localPosition.z);
    }

    internal bool LoadSave(string save)
    {
        Debug.Log($"Loading save {save}");
        if (!_initialized) Initialize();
        
        Stopwatch stopwatch = Stopwatch.StartNew();
        if (!VRCJson.TryDeserializeFromJson(save, out DataToken token) || token.TokenType != TokenType.DataDictionary)
        {
            Debug.LogError($"Unable to load save: {token.ToString()}");
            return false;
        }

        ResetPools();

        DataDictionary spawnPropTimes = new DataDictionary();
        DataDictionary dictionary = token.DataDictionary;
        DataList keys = dictionary.GetKeys();
        for (int i = 0; i < keys.Count; i++)
        {
            string template = keys[i].String;
            DataList props = dictionary[template].DataList;
            for (int j = 0; j < props.Count; j++)
            {
                DataList prop = props[j].DataList;
                Vector3 position = TokenToPosition(prop[0]);
                Quaternion rotation = TokenToRotation(prop[1]);
                DataDictionary parameters = prop.Count >= 3 ? prop[2].DataDictionary : null;
                SpawnProp(template, position, rotation, parameters);
            }
        }

        _loaded = true;
        return true;
    }
    
    //PropPools
        //Key(string name) = Value(DataList pool)
            //GameObject prop
    internal GameObject SpawnProp(string name, Vector3 position, Quaternion rotation, DataDictionary parameters = null)
    {
        name = name.ToLower();
        //Debug.Log($"Spawning prop {name} {position} {rotation}");
        if (!templates.ContainsKey(name))
        {
            Debug.LogError($"Template '{name}' not found");
            return null;
        }

        if (doRounding)
        {
            position = RoundPosition(position);
            rotation = RoundRotation(rotation);
        }
        if (_inactivePools[name].DataList.TryGetValue(0, out DataToken potentialProp) && !potentialProp.IsNull)
        {
            //Retrieve existing prop
            GameObject newProp = (GameObject)potentialProp.Reference;
            newProp.SetActive(true);
            newProp.transform.localPosition = position;
            newProp.transform.localRotation = rotation;
            _inactivePools[name].DataList.RemoveAt(0);
            _activePools[name].DataList.Add(newProp);
            WorldPropTemplate propTemplate = newProp.GetComponentInChildren<WorldPropTemplate>();
            if (Utilities.IsValid(propTemplate))
            {
                propTemplate.ApplyParameters(parameters);
            }
            return newProp;
        }
        else //If TryGetValue fails, that means we need to instantiate new props to expand the pool
        {
            //Instantiate new prop
            Transform template = GetTemplate(name);
            #if UNITY_EDITOR && !COMPILER_UDONSHARP
            GameObject newProp;
            if (FindPrefabPath(GetPrefabName(template.gameObject), out string path))
            {
                Object asset = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject));
                newProp = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            }
            else
            {
                newProp = Instantiate(template.gameObject);
            }
            #else
            GameObject newProp = Instantiate(template.gameObject);
            #endif
            newProp.transform.SetParent(transform);
            
            newProp.transform.localPosition = position;
            newProp.transform.localRotation = rotation;
            
            newProp.name = name;
            newProp.SetActive(true);
            WorldPropTemplate propTemplate = newProp.GetComponentInChildren<WorldPropTemplate>();
            _activePools[name].DataList.Add(newProp);
            if (Utilities.IsValid(propTemplate))
            {
                propTemplate.ApplyParameters(parameters);
            }
            return newProp;
        }
    }

    internal void ReturnProp(GameObject prop)
    {
        if (!Utilities.IsValid(prop)) return;
        if (!_activePools.ContainsKey(prop.name))
        {
            Debug.LogError($"Unable to return object '{prop.name}' as it's not a recognized prop");
            return;
        }
        DataList pool = _activePools[prop.name].DataList;
        int index = pool.IndexOf(prop);
        if (index == -1)
        {
            Debug.LogError($"Unable to return object '{prop.name}' as it's not a recognized prop");
            return;
        }
        
        pool.RemoveAt(index); //Remove from pool at current index
        _inactivePools[prop.name].DataList.Add(prop);
        prop.SetActive(false);
    }
    internal void ResetPools()
    {
        if (!_initialized) Initialize();
        var keys = _activePools.GetKeys();
        for (int i = 0; i < keys.Count; i++)
        {
            //Debug.Log(_activePools[keys[i]]);
            DataList activePool = _activePools[keys[i]].DataList;
            DataList inactivePool = _inactivePools[keys[i]].DataList;
            for (int j = 0; j < activePool.Count; j++)
            {
                GameObject prop = (GameObject)activePool[j].Reference;
                inactivePool.Add(prop);
                prop.SetActive(false);
            }
            activePool.Clear();
        }
    }


    public DataList GetTemplates()
    {
        return templates.GetKeys();
    }
    
    public Transform GetTemplate(string name)
    {
        if (!templates.ContainsKey(name)) return null;
        return (Transform)templates[name].Reference;
    }

    internal Vector3 TokenToPosition(DataToken token)
    {
        DataList list = token.DataList;
        return new Vector3((float)(list[0].Number / positionRounding), (float)(list[1].Number / positionRounding), (float)(list[2].Number / positionRounding));
    }

    internal DataToken PositionToToken(Vector3 vector)
    {
        vector = RoundPosition(vector);
        DataList list = new DataList();
        list.Add(Mathf.RoundToInt(vector.x * positionRounding));
        list.Add(Mathf.RoundToInt(vector.y * positionRounding));
        list.Add(Mathf.RoundToInt(vector.z * positionRounding));
        return list;
    }
    
    internal Quaternion TokenToRotation(DataToken token)
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

    internal DataToken RotationToToken(Quaternion rotation)
    {
        rotation = RoundRotation(rotation);
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

    internal Vector3 RoundPosition(Vector3 position)
    {
        if (!doRounding) return position;
        position = position / positionRounding;
        position.x = Mathf.RoundToInt(position.x);
        position.y = Mathf.RoundToInt(position.y);
        position.z = Mathf.RoundToInt(position.z);
        position = position * positionRounding;
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
    #if UNITY_EDITOR && !COMPILER_UDONSHARP
    
    //Helper for the below, attempts to find the canonical name of a prefab by stripping out the copy index (e.g. "Room_02 (1)" to "Room_02").
    //Not at all foolproof but I am lacking in better ideas.
    static string GetPrefabName(GameObject target)
    {
        string name = target.name.ToString();
        name = name.Replace("(Clone)", "");
        name = Regex.Replace(name, "\\s\\(\\d+\\)$", "");
        name = name.Trim();

        return name;
    }

    //Helper function to find a prefab with the exact given name in the Assets Database.
    //Returns success. Path = "" when not successful.
    static bool FindPrefabPath(string name, out string path)
    {
        string prefabName = name + ".prefab";
        string[] findResult = AssetDatabase.FindAssets(name);
        bool success = false;

        string foundPath = "";
        foreach (string guid in findResult)
        {
            string thisPath = AssetDatabase.GUIDToAssetPath(guid);
            string file = Path.GetFileName(thisPath);

            if (file.Equals(prefabName))
            {
                success = true;
                foundPath = thisPath;
                break;
            }
        }

        path = foundPath;

        return success;
    }
#endif
}
