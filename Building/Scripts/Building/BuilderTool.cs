
using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;
using VRDC_systems.Building.Scripts.Building;

/// <summary>
/// BuilderTool is the base class for all tools. This allows each individual tool to perform their own actions independently, while the registration with the builder and UI is handled
/// </summary>
public class BuilderTool : UdonSharpBehaviour
{
    public Sprite icon;
    [NonSerialized]
    public DataDictionary Properties = new DataDictionary();
    public DataList Actions = new DataList();
    internal Builder Builder;
    internal BuildManager BuildManager;

    public void CreateProperty(string key, DataToken value)
    {
        Properties[key] = Observable.Create(value);
    }

    public Observable GetProperty(string key)
    {
        if (!Properties.ContainsKey(key)) Debug.Log($"BuilderTool {name} couldn't find property {key}");
        return Properties[key].AsObservable();
    }

    public bool HasProperty(string key)
    {
        return Properties.ContainsKey(key);
    }
    public virtual void Initialize(Builder builder, BuildManager buildManager)
    {
        Builder = builder;
        BuildManager = buildManager;
    }

    public virtual void SetToolActive(bool active)
    {
        
    }

    public virtual void ToolUpdate()
    {
        
    }
    public virtual void PrimaryAction(bool down)
    {
        
    }

    public virtual void SecondaryAction(bool down)
    {
        
    }

    public virtual void Scroll(float change)
    {
        
    }
    
    public virtual Texture2D Icon()
    {
        return null;
    }
}
