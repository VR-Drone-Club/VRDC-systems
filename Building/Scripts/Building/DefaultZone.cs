
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDK3.StringLoading;
using VRC.SDKBase;
using VRC.Udon;

public class DefaultZone : ZoneProvider
{
    [UdonSynced]
    private string _syncedZone;

    private DataDictionary _currentZone;
    public ZoneData CurrentZone => (ZoneData)_currentZone;
    public VRCUrl DefaultURL;
    void Start()
    {
        Initialize();
        if (Networking.IsOwner(gameObject)) VRCStringDownloader.LoadUrl(DefaultURL, this);
    }

    public override void OnStringLoadSuccess(IVRCStringDownload result)
    {
        if (!Networking.IsOwner(gameObject)) return;
        Debug.Log($"String load successful {result.Result}");
        _syncedZone = result.Result;
        OnDeserialization();
        RequestSerialization();
    }

    internal override void Initialize()
    {
        if (_initialized) return;
        base.Initialize();
        _currentZone = ZoneData.Constructor(this, BuildManager.Instance());
        CurrentZone.PropsObservable().Subscribe(this, nameof(ZoneDataChanged));
        AddZone(CurrentZone);
        ZoneManager.Instance().LoadZone(CurrentZone);
    }

    public override void OnDeserialization()
    {
        Initialize();
        //Debug.Log("DefaultZone OnDeserialization");
        CurrentZone.PropsObservable().SetValue(_syncedZone); // set zone to match provider
    }

    public override void OnPreSerialization()
    {
        Initialize();
        //Debug.Log("DefaultZone OnPreserialization");
        _syncedZone = CurrentZone.Props(); // set provider to match zone
    }

    public void ZoneDataChanged()
    {
        //Debug.Log("DefaultZone ZoneDataChanged");
        if (_syncedZone == CurrentZone.Props()) return; // ignore detections that have already been applied
        if (!CurrentZone.CanEdit()) return; // ignore changes when you can't edit
        if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject); // change owner if necessary
        _syncedZone = CurrentZone.Props();
        RequestSerialization(); // send current state
        // WHY IS THIS BEHAVING LIKE CONTINUOUS
        //Debug.Log($"DefaultZone sent changes\n{_syncedZone}");
    }


    public override string Path()
    {
        return "default";
    }

    public override bool CanLoad(ZoneData zone)
    {
        return true;
    }

    public override bool CanEdit(ZoneData zone)
    {
        return true; // change this to false to lock it down
    }
}
