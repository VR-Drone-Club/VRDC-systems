
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
        foreach (var renderer in gameObject.GetComponentsInChildren<MeshFilter>(true))
        {
            if (!Utilities.IsValid(renderer)) continue;
            AddOutline(renderer);
        }
    }

    public void RemoveOutline(GameObject gameObject)
    {
        foreach (var filter in gameObject.GetComponentsInChildren<MeshFilter>(true))
        {
            if (!Utilities.IsValid(filter)) continue;
            RemoveOutline(filter);
        }
    }

    public void AddOutline(MeshFilter filter)
    {
        Debug.Log($"AddOutline {filter} {_activeSelections.ContainsKey(filter)}");
        if (_activeSelections.ContainsKey(filter)) return;
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
        outline.transform.SetParent(filter.transform);
        outline.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        outline.transform.localScale = Vector3.one;
        outline.GetComponent<MeshFilter>().mesh = filter.mesh;
        _activeSelections[filter] = outline;
    }

    public void RemoveOutline(MeshFilter filter)
    {
        if (!_activeSelections.ContainsKey(filter)) return;
        GameObject outline = (GameObject)_activeSelections[filter].Reference;
        outline.transform.SetParent(transform);
        outline.SetActive(false);
        _activeSelections.Remove(filter);
        _selectionPool.Add(outline);
    }
}
