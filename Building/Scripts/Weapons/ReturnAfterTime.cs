
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ReturnAfterTime : UdonSharpBehaviour
{
    public float returnTime;
    public GenericPool pool;
    private void OnEnable()
    {
        SendCustomEventDelayedSeconds(nameof(Return), returnTime);
    }

    public void Return()
    {
        pool.ReturnProp(gameObject);
    }
}
