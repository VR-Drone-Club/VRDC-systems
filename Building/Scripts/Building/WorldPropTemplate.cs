
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

public class WorldPropTemplate : UdonSharpBehaviour
{
    public bool lengthResizable;
    public bool heightResizable;
    public int lengthOffset;
    public int minLength;
    public int maxLength;
    public int heightOffset;
    public int minHeight;
    public int maxHeight;
    public Transform resizeTransform;
    [NonSerialized]
    public DataDictionary currentParameters = new DataDictionary();

    public bool hasSprite;
    public float spriteOrder;
    public Sprite sprite;
    public Vector3 spriteOffsetPosition;
    public Vector3 spriteOffsetRotation;
    public Vector3 spriteScale;
    [NonSerialized] public BuildManager BuildManager;
    
    public virtual void ResetParameters()
    {
        currentParameters.Clear();
        if (lengthResizable)
        {
            ApplyLength(minLength);
        }
        if (heightResizable)
        {
            ApplyHeight(minHeight);
        }
    }

    public void ApplyParameters(DataDictionary parameters)
    {
        ResetParameters();
        if (!Utilities.IsValid(parameters)) return;
        currentParameters = parameters.DeepClone();
        if (lengthResizable && parameters.TryGetValue("Length", out DataToken length) && length.IsNumber)
        {
            ApplyLength(Mathf.RoundToInt((float)length.Number));
        }
        if (heightResizable && parameters.TryGetValue("Height", out DataToken height) && height.IsNumber)
        {
            ApplyHeight(Mathf.RoundToInt((float)height.Number));
        }
        DeserializeProp(parameters);
    }
    /// <summary>
    /// This function is your opportunity to build custom behavior onto a prop.
    /// When this prop has parameters associated with it, they are synced and saved in the system.
    /// When those parameters are then loaded back onto the object, they are passed through this function.
    /// Therefore, you can override it and use it to apply the data in parameters onto the prop itself.
    /// Local variables that exist in this script will not be saved or synced, only data stored as a parameter will be saved and synced.
    /// </summary>
    /// <param name="parameters"></param>
    public virtual void DeserializeProp(DataDictionary parameters)
    {
    }

    public DataDictionary GetParameters()
    {
        
        if (!Utilities.IsValid(currentParameters)) currentParameters = new DataDictionary();
        return SerializeProp();
    }
    /// <summary>
    /// This function is your opportunity to build custom behavior onto a prop.
    /// When the system managing this prop asks it to package up its data, it goes through this function.
    /// You can override this function and use it to set parameters that you want to be saved.
    /// Later, this prop will receive DeserializeProp with the same parameters that you gave it here.
    /// </summary>
    /// <returns></returns>
    public virtual DataDictionary SerializeProp()
    {
        return currentParameters;
    }

    public virtual DataList GetSpriteData()
    {
        if (!hasSprite || !Utilities.IsValid(sprite)) return null;
        DataList list = new DataList();
        list.Add(spriteOrder);
        list.Add(sprite.name);
        list.Add(spriteOffsetPosition.ToDataToken());
        list.Add(Quaternion.Euler(spriteOffsetRotation).ToDataToken());
        list.Add(spriteScale.ToDataToken());
        return list;
    }

    public void ApplyLength(int length)
    {
        if (length < minLength) length = minLength;
        if (length > maxLength) length = maxLength;
        currentParameters["Length"] = length;
        Vector3 scale = resizeTransform.localScale;
        Vector3 position = resizeTransform.localPosition;
        resizeTransform.localScale = new Vector3(scale.x, scale.y, length * 0.2f);
        resizeTransform.localPosition = new Vector3(position.x, position.y, (length * 0.1f) + (lengthOffset / 5f));
    }

    public void ApplyHeight(int height)
    {
        if (height < minHeight) height = minHeight;
        if (height > maxHeight) height = maxHeight;
        currentParameters["Height"] = height;
        Vector3 scale = resizeTransform.localScale;
        Vector3 position = resizeTransform.localPosition;
        resizeTransform.localScale = new Vector3(scale.x, height * 0.2f, scale.z);
        resizeTransform.localPosition = new Vector3(position.x, height * 0.1f + (heightOffset / 5f), position.z);
    }

    internal int GetIntParameter(string key, int defaultValue)
    {
        if (!Utilities.IsValid(currentParameters)) return defaultValue;
        if (!currentParameters.ContainsKey(key)) return defaultValue;
        return Mathf.RoundToInt((float)currentParameters[key].Number);
    }
    internal string GetStringParameter(string key, string defaultValue)
    {
        if (!Utilities.IsValid(currentParameters)) return defaultValue;
        if (!currentParameters.ContainsKey(key)) return defaultValue;
        return currentParameters[key].String;
    }
    internal DataList GetListParameter(string key)
    {
        if (!Utilities.IsValid(currentParameters)) return new DataList();
        if (!currentParameters.ContainsKey(key)) return new DataList();
        return currentParameters[key].DataList;
    }

    internal void SetIntParameter(string key, int value, int defaultValue)
    {
        if (!Utilities.IsValid(currentParameters)) currentParameters = new DataDictionary();
        if (value == defaultValue)
            currentParameters.Remove(key);
        else
            currentParameters[key] = value;
    }
    /// <summary>
    /// Set a string parameter in the persistent storage for this prop
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="defaultValue"></param>
    internal void SetStringParameter(string key, string value, string defaultValue)
    {
        if (!Utilities.IsValid(currentParameters)) currentParameters = new DataDictionary();
        if (value == defaultValue)
            currentParameters.Remove(key);
        else
            currentParameters[key] = value;
    }
    /// <summary>
    /// Set a list parameter in the persistent storage for this prop
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="defaultValue"></param>
    internal void SetListParameter(string key, DataList value)
    {
        if (!Utilities.IsValid(currentParameters)) currentParameters = new DataDictionary();
        if (value == null || value.Count == 0)
            currentParameters.Remove(key);
        else
            currentParameters[key] = value;
    }

    /// <summary>
    /// Not all props need to be uniquely identified.
    /// But when they do, you can call this method at any point on a prop to give it a unique ID.
    /// This allows it to be found later by the same UUID.
    /// Other scripts or props can hold onto that UUID so they know how to get back to it.
    /// </summary>
    internal string GetUUID()
    {
        if (currentParameters.ContainsKey("uuid")) return currentParameters["uuid"].String;
        string uuid = GetNewHash();
        SetStringParameter("uuid", uuid, string.Empty);
        return uuid;
    }
    
    private string GetNewHash()
    {
        System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
        int time = (int)(System.DateTime.UtcNow - epochStart).TotalSeconds;
        return CalculateHash(time.ToString() + Networking.LocalPlayer.displayName + Time.realtimeSinceStartup).ToString();
    }
    private UInt32 CalculateHash(string read)
    {
        UInt32 hashedValue = 30744573;
        for(int i=0; i < read.Length; i++)
        {
            hashedValue += read[i];
            hashedValue *= 30744573; // scramble 
        }
        return hashedValue;
    }
}
