
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRCLightVolumes;

public class DynamicLightPool : UdonSharpBehaviour
{
    public Transform parent;
    private LightVolumeInstance[] children;
    private bool _initialized;

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (_initialized) return;
        children = GetComponentsInChildren<LightVolumeInstance>(true);
        foreach (var child in children)
        {
            child.gameObject.SetActive(false);
        }
    }

    public bool TryToSpawn(out LightVolumeInstance lightVolumeInstance)
    {
        Initialize();
        foreach (var child in children)
        {
            if (!child.gameObject.activeSelf)
            {
                Debug.Log("[DynamicLightPool] successfully spawned dynamic light");
                child.gameObject.SetActive(true);
                lightVolumeInstance = child;
                return true;
            }
        }
        Debug.Log("[DynamicLightPool] unable to find available dynamic light");
        lightVolumeInstance = null;
        return false;
    }

    public void ReturnToPool(LightVolumeInstance lightVolumeInstance)
    {
        Debug.Log($"[DynamicLightPool] returning {lightVolumeInstance.name} to pool");
        foreach (var child in children)
        {
            if (child == lightVolumeInstance)
            {
                child.transform.parent = parent;
                child.gameObject.SetActive(false);
            }
        }
    }
}
