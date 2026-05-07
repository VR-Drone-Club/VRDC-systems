
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

public class Target : UdonSharpBehaviour
{
    [NonSerialized]
    public bool active = true;
    private DataList _listeners = new DataList();

    public void Subscribe(Component listener)
    {
        if (_listeners == null)
        {
            _listeners = new DataList();
        }
        _listeners.Add(listener);
    }

    private void OnParticleCollision(GameObject other)
    {
        if (!active) return;
        if (!Utilities.IsValid(other)) return;
        Trigger();
    }

    private void Trigger()
    {
        for (int i = 0; i < _listeners.Count; i++)
        {
            if (_listeners[i].IsNull) continue;
            UdonBehaviour behaviour = (UdonBehaviour)_listeners[i].Reference;
            behaviour.SendCustomEvent("OnTargetHit");
        }
    }
    
}
