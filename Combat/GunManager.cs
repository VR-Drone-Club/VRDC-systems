
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;
using VRC.Udon;

public class GunManager : UdonSharpBehaviour
{
    public TargetingSystem targetingSystem;
    public AutoGun gun;
    void Start()
    {
        if (!Networking.IsOwner(gameObject)) return;
        targetingSystem.AssignGun(gun);
    }
}
