
using System;
using UdonSharp;
using UdonToolkit;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
[CustomEditor(typeof(ZoneNode))]
public class ZoneNodeEditor : UTEditor{}
#endif
public class ZoneNode : WorldPropTemplate
{
    public string name;
    public Color color = Color.black;
    public string[] linkNames = new string[0];
    private ZoneNode[] linkedNodes = new ZoneNode[0];

    public override DataDictionary SerializeProp()
    {
        DataList links = new DataList();
        foreach (var link in linkNames)
        {
            links.Add(link);
        }

        DataDictionary parameters = new DataDictionary();
        parameters["Name"] = name;
        parameters["Color"] = color.ToDataToken();
        parameters["Links"] = links;
        return parameters;
    }

    public override void DeserializeProp(DataDictionary parameters)
    {
        if (!parameters.TryGetDataList("Links", out DataList links)) return;
        if (!parameters.TryGetString("Name", out name)) return;
        if (!parameters.TryGetValue("Color", out DataToken colorToken)) return;
        color = colorToken.ToColor();
        linkNames = new string[links.Count];
        for (int i = 0; i < links.Count; i++)
        {
            linkNames[i] = links[i].String;
        }
        linkedNodes = new ZoneNode[linkNames.Length];
    }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
    private void OnDrawGizmos()
    {
        if (linkedNodes.Length != linkNames.Length) linkedNodes = new ZoneNode[linkNames.Length];
        Gizmos.color = Color.black;
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.black;
        Handles.color = color; 
        Handles.DrawSolidDisc(transform.position, Vector3.up, 50);
        Handles.Label(transform.position, name, style);
        Handles.color = Color.black;
        for (int i = 0; i < linkNames.Length; i++)
        {
            if (Utilities.IsValid(linkedNodes[i] && linkedNodes[i].name != linkNames[i])) linkedNodes[i] = FindNode(linkNames[i]);
            if (!Utilities.IsValid(linkedNodes[i])) linkedNodes[i] = FindNode(linkNames[i]);
            if (!Utilities.IsValid(linkedNodes[i])) continue;
            Vector3 a = transform.position;
            Vector3 b = linkedNodes[i].transform.position;
            Handles.DrawLine(Vector3.MoveTowards(a, b, 100), Vector3.MoveTowards(b, a, 100));
        }
    }

    private ZoneNode FindNode(string name)
    {
        var nodes = transform.parent.GetComponentsInChildren<ZoneNode>();
        foreach (var node in nodes)
        {
            if (!Utilities.IsValid(node)) continue;
            if (node.name == name) return node;
        }
        
        return null;
    }
    
    #endif
}
