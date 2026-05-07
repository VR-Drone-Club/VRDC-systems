
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRCLightVolumes;

public class DroneSpaceConverter : UdonSharpBehaviour
{
    public Transform simulationSpace;
    public Transform mover;
    public EffectPicker effectPicker;
    public DynamicLightPool dynamicLightPool;
    private LightVolumeInstance dynamicLight;
    private VRCDroneApi _droneApi;
    private VRCPlayerApi _playerApi;
    void Start()
    {
        _playerApi = Networking.GetOwner(gameObject);
        _droneApi = _playerApi.GetDrone(); 
        effectPicker.AssignTrail(Networking.GetOwner(gameObject), mover, simulationSpace);
        Tick();
    }

    public void Tick()
    {
        #if UNITY_EDITOR
        
        #else
        if (!_droneApi.IsDeployed())
        {
            if (mover.gameObject.activeSelf) StateChanged(false);
            SendCustomEventDelayedSeconds(nameof(Tick), 1);
            return;
        }
#endif
        if (!mover.gameObject.activeSelf) StateChanged(true);
        #if UNITY_EDITOR
        mover.transform.SetLocalPositionAndRotation(_playerApi.GetPosition(), _playerApi.GetRotation());
        #else
        mover.transform.SetLocalPositionAndRotation(_droneApi.GetPosition(), _droneApi.GetRotation());
#endif
        SendCustomEventDelayedSeconds(nameof(Tick), 0);
    }

    public void StateChanged(bool value)
    {
        mover.gameObject.SetActive(value);
        if (value && !Utilities.IsValid(dynamicLight) && dynamicLightPool.TryToSpawn(out LightVolumeInstance lightVolumeInstance))
        {
            dynamicLight = lightVolumeInstance;
            dynamicLight.transform.SetParent(mover);
            dynamicLight.transform.localPosition = Vector3.zero;
            ColorApplicator colorApplicator = dynamicLight.GetComponent<ColorApplicator>();
            if (Utilities.IsValid(colorApplicator)) colorApplicator.Apply(_playerApi);
        }
        else if (!value && Utilities.IsValid(dynamicLight))
        {
            dynamicLightPool.ReturnToPool(dynamicLight);
            dynamicLight = null;
        }
    }

    private void OnDestroy()
    {
        if (Utilities.IsValid(dynamicLight))
        {
            dynamicLightPool.ReturnToPool(dynamicLight);
        }
    }
}
