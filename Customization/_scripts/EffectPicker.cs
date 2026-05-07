
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
    private DataDictionary _trailSimulationSpaces = new DataDictionary();
    private DataDictionary _trailSizes = new DataDictionary();
    private DataDictionary _inverseAssignmentMap = new DataDictionary();
    private DataDictionary _spawnedTrails = new DataDictionary();

    private void Start()
    {
        #if UDONSHELL
        if (!Utilities.IsValid(UdonShellReferenceManager.Instance())) return;
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
        DataList attachments = _assignedTrails[player.displayName].DataList;
        DataList simulationSpaces = _trailSimulationSpaces[player.displayName].DataList;
        DataList sizes = _trailSizes[player.displayName].DataList;
        for (int i = attachments.Count - 1; i >= 0; i--)
        {
            AssignTrail(player, (Transform)attachments[i].Reference, (Transform)simulationSpaces[i].Reference, sizes[i].Float);
        }
    }

    public void AssignTrail(VRCPlayerApi player, Transform attachmentPoint)
    {
        AssignTrail(player, attachmentPoint, null, 1);
    }
    public void AssignTrail(VRCPlayerApi player, Transform attachmentPoint, float size)
    {
        AssignTrail(player, attachmentPoint, null, size);
    }
    public void AssignTrail(VRCPlayerApi player, Transform attachmentPoint, Transform simulationSpace)
    {
        AssignTrail(player, attachmentPoint, simulationSpace, 1);
    }
    public void AssignTrail(VRCPlayerApi player, Transform attachmentPoint, Transform simulationSpace, float size)
    {
        if (!Utilities.IsValid(player) || !Utilities.IsValid(attachmentPoint)) return;
        Debug.Log($"Trail for {player.displayName} assigned to {attachmentPoint}");
        RemoveTrail(attachmentPoint);
        string displayName = player.displayName;
        if (!_assignedTrails.ContainsKey(player.displayName))
        {
            _assignedTrails[displayName] = new DataList();
            _trailSimulationSpaces[displayName] = new DataList();
            _trailSizes[displayName] = new DataList();
        }
        _assignedTrails[displayName].DataList.Add(attachmentPoint);
        _trailSimulationSpaces[displayName].DataList.Add(simulationSpace);
        _trailSizes[displayName].DataList.Add(size);
        _inverseAssignmentMap[attachmentPoint] = displayName;
        
        if (_chosenTrail.ContainsKey(player.displayName))
        {
            byte value = _chosenTrail[player.displayName].Byte;
            Transform trail = trailTemplates.GetChild(Mathf.Clamp(value, 0, trailTemplates.childCount - 1));
            var newObj = Instantiate(trail.gameObject, attachmentPoint.position, attachmentPoint.rotation, attachmentPoint);
            if (!Utilities.IsValid(newObj)) return;
            newObj.transform.localScale = trail.localScale * size;
            _spawnedTrails[attachmentPoint] = newObj;
            SetSimulationSpace(newObj.transform, simulationSpace);
            ColorApplicator colorApplicator = newObj.GetComponent<ColorApplicator>();
            if (!Utilities.IsValid(colorApplicator)) return;
            colorApplicator.Apply(player);
        }
    }

    private void SetSimulationSpace(Transform spawnedTrail, Transform simulationSpace)
    {
        if (!Utilities.IsValid(spawnedTrail) || !Utilities.IsValid(simulationSpace)) return;
        ParticleSystem[] particleSystems = spawnedTrail.GetComponentsInChildren<ParticleSystem>();
        for (int i = 0; i < particleSystems.Length; i++)
        {
            if (!Utilities.IsValid(particleSystems[i])) continue;
            var main = particleSystems[i].main;
            if (main.simulationSpace == ParticleSystemSimulationSpace.Local) continue;
            main.simulationSpace = ParticleSystemSimulationSpace.Custom;
            main.customSimulationSpace = simulationSpace;
        }
    }

    public void RemoveTrail(Transform trailAttachment)
    {
        if (!_inverseAssignmentMap.ContainsKey(trailAttachment)) return; //trail is not assigned anywhere, ignore
        string playerName = _inverseAssignmentMap[trailAttachment].String; //get player name for this trail
        if (_assignedTrails.ContainsKey(playerName))
        {
            DataList trails = _assignedTrails[playerName].DataList; //get list associated with player
            DataList spaces = _trailSimulationSpaces[playerName].DataList; //get list associated with player
            DataList sizes = _trailSizes[playerName].DataList; //get list associated with player
            var index = trails.IndexOf(trailAttachment);
            if (index >= 0)
            {
                trails.RemoveAt(index); //remove from list
                spaces.RemoveAt(index); //remove from list
                sizes.RemoveAt(index); //remove from list
            }
        }

        if (_spawnedTrails.ContainsKey(trailAttachment))
        {
            Object foundTrail = (Object)_spawnedTrails[trailAttachment].Reference;
            Destroy(foundTrail);
            _spawnedTrails.Remove(trailAttachment);
        }
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
        if (Utilities.IsValid(colorApplicator)) colorApplicator.Apply(player);
        ParticleSystem particleSystem = newObject.GetComponent<ParticleSystem>();
        if (Utilities.IsValid(particleSystem)) particleSystem.Play();
        return particleSystem;
    }
}
