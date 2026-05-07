
using UdonSharp;
using UdonToolkit;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
[CustomEditor(typeof(MeshSwapper))]
public class MeshSwapperEditor : UTEditor { }
#endif  
public class MeshSwapper : WorldPropTemplate
{
    [ListView("Meshes")]
    public Mesh[] visualMeshes;
    [ListView("Meshes")]
    public Mesh[] collisionMeshes;
    [Popup(nameof(GetMeshNames))]
    public string mesh;

    private MeshCollider _collider;
    private MeshFilter _meshFilter;

    public string[] GetMeshNames()
    {
        string[] meshNames = new string[visualMeshes.Length];
        for (int i = 0; i < visualMeshes.Length; i++)
        {
            if (!Utilities.IsValid(visualMeshes[i])) continue;
            meshNames[i] = visualMeshes[i].name;
        }
        return meshNames;
    }
    [Button("Apply Mesh")]
    public void ApplyMesh()
    {
        if (!Utilities.IsValid(_collider)) _collider = GetComponentInChildren<MeshCollider>();
        if (!Utilities.IsValid(_meshFilter)) _meshFilter = GetComponentInChildren<MeshFilter>();

        int index = -1;
        for (int i = 0; i < visualMeshes.Length; i++)
        {
            if (!Utilities.IsValid(visualMeshes[i])) continue;
            if (visualMeshes[i].name == mesh)
            {
                index = i;
                break;
            }
        }

        if (index == -1)
        {
            Debug.LogError($"MeshSwapper unable to find mesh '{mesh}'");
            return;
        }
        if (Utilities.IsValid(_collider))
        {
            _collider.sharedMesh = collisionMeshes[index];
        }

        if (Utilities.IsValid(_meshFilter))
        {
            _meshFilter.sharedMesh = visualMeshes[index];
        }
    }
    
    public override DataDictionary SerializeProp()
    {
        if (!Utilities.IsValid(currentParameters)) currentParameters = new DataDictionary();
        SetStringParameter(nameof(mesh), mesh, string.Empty);
        return currentParameters;
    }

    public override void DeserializeProp(DataDictionary parameters)
    {
        currentParameters = parameters;
        mesh = GetStringParameter(nameof(mesh), string.Empty);
        ApplyMesh();
    }
}
