
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

public class ZoneFileSystem : ZoneProvider
{
    public BuildManager buildManager;
    public MenuBarRegistry menuBarRegistry;
    void Start()
    {
        menuBarRegistry.RegisterMenuItem("Save", this, nameof(Save));
    }

    public void Save()
    {
        string save = buildManager.ExportSave();
        var zone = ZoneData.Constructor(this, save);
        AddZone(zone);
    }
}
