
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ParticleDetector : UdonSharpBehaviour
{
    public AudioClip hitClip;
    void Start()
    {
        
    }

    private void OnParticleCollision(GameObject other)
    {
        Debug.Log("Particle hit");
        AudioSource.PlayClipAtPoint(hitClip, Networking.LocalPlayer.GetPosition());
    }
}
