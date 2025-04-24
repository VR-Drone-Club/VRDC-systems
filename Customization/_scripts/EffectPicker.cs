
using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDK3.Data;
using VRC.SDK3.Persistence;
using VRC.SDKBase;
using VRC.Udon;
using Object = UnityEngine.Object;

public class EffectPicker : UdonSharpBehaviour
{
    public Transform trailTemplates;
    public Transform burstTemplates;
    private DataDictionary _chosenTrail = new DataDictionary();
    private DataDictionary _chosenBurst = new DataDictionary();
    private DataDictionary _assignedTrails = new DataDictionary();
    private DataDictionary _inverseAssignmentMap = new DataDictionary();
    private DataDictionary _spawnedTrails = new DataDictionary();

    private void Start()
    {
        #if UDONSHELL
        UdonShellCore core = UdonShellReferenceManager.Instance().udonShellCore;
        core.RegisterFunction(this, nameof(SetTrail), "Player Manipulation")
            .WithArgument(nameof(targeted), "target")
            .WithArgument(nameof(value), "number");

        core.RegisterFunction(this, nameof(SetBurst), "Player Manipulation")
            .WithArgument(nameof(targeted), "target")
            .WithArgument(nameof(value), "number");

        core.RegisterFunction(this, nameof(PlayBurst), "Player Manipulation")
            .WithArgument(nameof(burstPlayer), "unique_target")
            .WithArgument(nameof(burstPosition), "vector3")
            .WithArgument(nameof(burstRotation), "rotation")
            .WithArgument(nameof(burstScale), "number");
        #endif
    }

    public static EffectPicker Instance()
    {
        GameObject foundObject = GameObject.Find(nameof(EffectPicker));
        if (!Utilities.IsValid(foundObject)) return null;
        return foundObject.GetComponent<EffectPicker>();
    }

    public override void OnPlayerRestored(VRCPlayerApi player)
    {
        ReadEffects(player);
    }

    public override void OnPlayerDataUpdated(VRCPlayerApi player, PlayerData.Info[] infos)
    {
        ReadEffects(player);
    }

    public bool targeted;
    public float value;
    public void SetTrail()
    {
        if (!targeted) return;
        byte value = (byte)(Mathf.Clamp(Mathf.RoundToInt(this.value), byte.MinValue, byte.MaxValue));
        PlayerData.SetByte("Effect_Trail", value);
    }

    public void SetBurst()
    {
        if (!targeted) return;
        byte value = (byte)(Mathf.Clamp(Mathf.RoundToInt(this.value), byte.MinValue, byte.MaxValue));
        PlayerData.SetByte("Effect_Burst", value);
    }
    private void ReadEffects(VRCPlayerApi player)
    {
        if (PlayerData.HasKey(player, "Effect_Trail")) _chosenTrail[player.displayName] = PlayerData.GetByte(player, "Effect_Trail");
        if (PlayerData.HasKey(player, "Effect_Burst")) _chosenBurst[player.displayName] = PlayerData.GetByte(player, "Effect_Burst");
        EffectChanged(player);
    }

    public void IncrementBurst(int change)
    {
        if (burstTemplates.childCount == 0) return;
        int current = GetBurst(Networking.LocalPlayer);
        current += change;
        Debug.Log($"Incrementing burst by {change}, new {current}, max {burstTemplates.childCount}");
        if (current < 0) current = burstTemplates.childCount - current;
        current %= burstTemplates.childCount;
        value = current;
        targeted = true;
        SetBurst();
    }
    public void IncrementTrail(int change)
    {
        int current = GetTrail(Networking.LocalPlayer);
        current += change;
        if (current < 0) current = trailTemplates.childCount - current;
        current %= trailTemplates.childCount;
        value = current;
        targeted = true;
        SetTrail();
    }
    
    public byte GetBurst(VRCPlayerApi player)
    {
        if (PlayerData.TryGetByte(player, "Effect_Burst", out byte value))
        {
            return value;
        }
        return 0;
    }

    public byte GetTrail(VRCPlayerApi player)
    {
        if (PlayerData.TryGetByte(player, "Effect_Trail", out byte value))
        {
            return value;
        }
        return 0;
    }
    
    public string GetBurstName(VRCPlayerApi player)
    {
        if (PlayerData.TryGetByte(player, "Effect_Burst", out byte value) && value < burstTemplates.childCount)
        {
            return burstTemplates.GetChild(value).name;
        }
        return "None";
    }
    public string GetTrailName(VRCPlayerApi player)
    {
        if (PlayerData.TryGetByte(player, "Effect_Trail", out byte value) && value < trailTemplates.childCount)
        {
            return trailTemplates.GetChild(value).name;
        }
        return "None";
    }
    private void EffectChanged(VRCPlayerApi player)
    {
        if (!_assignedTrails.ContainsKey(player.displayName)) return;
        DataList list = _assignedTrails[player.displayName].DataList;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            Transform attachment = (Transform)list[i].Reference;
            AssignTrail(player, attachment);
        }
    }
    
    public void AssignTrail(VRCPlayerApi player, Transform attachmentPoint)
    {
        if (!Utilities.IsValid(player) || !Utilities.IsValid(attachmentPoint)) return;
        RemoveTrail(attachmentPoint);
        string displayName = player.displayName;
        if (!_assignedTrails.ContainsKey(player.displayName))
        {
            _assignedTrails[displayName] = new DataList();
        }
        _assignedTrails[displayName].DataList.Add(attachmentPoint);
        _inverseAssignmentMap[attachmentPoint] = displayName;
        
        if (_chosenTrail.ContainsKey(player.displayName))
        {
            byte value = _chosenTrail[player.displayName].Byte;
            Transform trail = trailTemplates.GetChild(Mathf.Clamp(value, 0, trailTemplates.childCount - 1));
            var newObj = Instantiate(trail.gameObject, attachmentPoint.position, attachmentPoint.rotation, attachmentPoint);
            if (!Utilities.IsValid(newObj)) return;
            _spawnedTrails[attachmentPoint] = newObj;
            ColorApplicator colorApplicator = newObj.GetComponent<ColorApplicator>();
            if (!Utilities.IsValid(colorApplicator)) return;
            colorApplicator.SetPlayer(player);
        }
    }

    public void RemoveTrail(Transform trailAttachment)
    {
        if (!_inverseAssignmentMap.ContainsKey(trailAttachment)) return; //trail is not assigned anywhere, ignore
        string playerName = _inverseAssignmentMap[trailAttachment].String; //get player name for this trail
        if (!_assignedTrails.ContainsKey(playerName)) return; //trail is not assigned, ignore
        DataList list = _assignedTrails[playerName].DataList; //get list associated with player
        list.Remove(trailAttachment); //remove from list
        if (!_spawnedTrails.ContainsKey(trailAttachment)) return; //trail is not spawned, no need to clean up
        Object foundTrail = (Object)_spawnedTrails[trailAttachment].Reference;
        Destroy(foundTrail);
        _spawnedTrails.Remove(trailAttachment);
    }

    public VRCPlayerApi burstPlayer;
    public Vector3 burstPosition;
    public Quaternion burstRotation;
    public float burstScale;
    
    public void PlayBurst()
    {
        Burst(burstPlayer, burstPosition, burstRotation, burstScale);
    }
    public ParticleSystem Burst(VRCPlayerApi player, Vector3 position, Quaternion rotation, float scale = 1)
    {
        if (!Utilities.IsValid(player))
        {
            Debug.Log("Unable to burst: provided player was invalid");
            return null;
        }
        if (!_chosenBurst.ContainsKey(player.displayName)) return null;
        if (burstTemplates.childCount == 0) return null;
        byte value = _chosenBurst[player.displayName].Byte;
        Transform burst = burstTemplates.GetChild(Mathf.Clamp(value, 0, burstTemplates.childCount - 1));
        var newObject = Instantiate(burst.gameObject, position, rotation);
        if (!Utilities.IsValid(newObject)) return null;
        scale *= Vector3.Distance(Networking.LocalPlayer.GetPosition(), position) / 5;
        newObject.transform.localScale *= scale;
        ColorApplicator colorApplicator = newObject.GetComponent<ColorApplicator>();
        if (Utilities.IsValid(colorApplicator)) colorApplicator.SetPlayer(player);
        ParticleSystem particleSystem = newObject.GetComponent<ParticleSystem>();
        if (Utilities.IsValid(particleSystem)) particleSystem.Play();
        return particleSystem;
    }
}
