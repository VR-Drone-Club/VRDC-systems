
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

public class OutlineUtility : UdonSharpBehaviour
{

    private DataList _selectionPool = new DataList();
    private DataDictionary _activeSelections = new DataDictionary();
    
    public GameObject selectionTemplate;
    public void AddOutline(GameObject gameObject)
    {
        foreach (var renderer in gameObject.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (!Utilities.IsValid(renderer)) continue;
            AddOutline(renderer);
        }
    }

    public void RemoveOutline(GameObject gameObject)
    {
        foreach (var renderer in gameObject.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (!Utilities.IsValid(renderer)) continue;
            RemoveOutline(renderer);
        }
    }

    public void AddOutline(MeshRenderer renderer)
    {
        MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
        GameObject outline;
        if (_selectionPool.Count > 0)
        {
            outline = (GameObject)_selectionPool[0].Reference;
            _selectionPool.RemoveAt(0);
        }
        else
        {
            outline = Instantiate(selectionTemplate);
        }
        outline.gameObject.SetActive(true);
        outline.transform.SetParent(renderer.transform);
        outline.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        outline.transform.localScale = Vector3.one;
        outline.GetComponent<MeshFilter>().mesh = meshFilter.mesh;
        _activeSelections[renderer] = outline;
    }

    public void RemoveOutline(MeshRenderer renderer)
    {
        if (!_activeSelections.ContainsKey(renderer)) return;
        GameObject outline = (GameObject)_activeSelections[renderer].Reference;
        outline.transform.SetParent(transform);
        outline.SetActive(false);
        _activeSelections.Remove(renderer);
        _selectionPool.Add(outline);
    }
}
