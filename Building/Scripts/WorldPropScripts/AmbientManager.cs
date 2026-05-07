
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

public static class AmbientManagerFinder
{
    public static AmbientManager FindAmbientManager()
    {
        GameObject gameObject = GameObject.Find(nameof(AmbientManager));
        if (!Utilities.IsValid(gameObject)) return null;
        return gameObject.GetComponent<AmbientManager>();
    }
}
public class AmbientManager : UdonSharpBehaviour
{

    private DataList currentZones = new DataList();
    private DataDictionary currentProperties = new DataDictionary();
    
    public void AddZone(AmbientZone zone)
    {
        if (currentZones.Contains(zone)) return;
        currentZones.Add(zone);
        EvaluateZones();
    }

    public void RemoveZone(AmbientZone zone)
    {
        currentZones.Remove(zone);
        EvaluateZones();
    }

    private void EvaluateZones()
    {
        SortByPriority(currentZones);
        bool appliedFog = false;
        for (int i = currentZones.Count - 1; i >= 0; i--)
        {
            if (appliedFog) break;
            AmbientZone zone = (AmbientZone)currentZones[i].Reference;
            if (!appliedFog && zone.controlFog)
            {
                appliedFog = true;
                RenderSettings.fog = true;
                RenderSettings.fogColor = zone.fogColor;
                RenderSettings.fogDensity = zone.fogDensity;
            }
        }

        if (!appliedFog)
        {
            RenderSettings.fog = false;
        }
    }

    private void SortByPriority(DataList zoneList)
    {
        var n = zoneList.Count;
        for (int i = 0; i < n - 1; i++)
        {
            for (int j = 0; j < n - i - 1; j++)
            {
                if (CompareZones(zoneList[j], zoneList[j + 1]))
                {
                    var tempVar = zoneList[j];
                    zoneList[j] = zoneList[j + 1];
                    zoneList[j + 1] = tempVar;
                }
            }
        }
    }
    private bool CompareZones(DataToken a, DataToken b)
    {
        AmbientZone objectA = (AmbientZone)a.Reference;
        AmbientZone objectB = (AmbientZone)b.Reference;
        return (objectA.priority > objectB.priority);
    }
    
    private Color TokenToColor(DataToken token)
    {
        Color color = new Color();
        color.r = (float)token.DataList[0].Number;
        color.g = (float)token.DataList[1].Number;
        color.b = (float)token.DataList[2].Number;
        color.a = (float)token.DataList[3].Number;
        return color;
    }

    private DataToken ColorToToken(Color color)
    {
        DataList list = new DataList();
        list.Add(color.r);
        list.Add(color.g);
        list.Add(color.b);
        list.Add(color.a);
        return list;
    }
}
