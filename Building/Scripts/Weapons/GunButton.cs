
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class GunButton : UdonSharpBehaviour
{
    public UdonBehaviour target;
    public string sendEvent;
    void Start()
    {
        
    }

    public override void Interact()
    {
        target.SendCustomEvent(sendEvent);
    }
}