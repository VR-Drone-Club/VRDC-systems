
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class OwnershipDebugging : UdonSharpBehaviour
{
    public Material[] Materials;
    public Renderer Renderer;
    public ParticleSystem Particles;

    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        Renderer.material = Materials[player.playerId % Materials.Length];
        Particles.Play();
    }
}
