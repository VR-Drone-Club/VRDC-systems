
using Phasedragon.AdminUtilities;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Fishbowl : UdonSharpBehaviour
{
    public Transform target;
    public AnimationCurve _strengthCurve;
    public Rigidbody rigidBody;
    public float _pushStrength = 1;
    public float _strength = 200;
    public float _lerp = 1;
    public float _dampening = 40;
    public float _threshold = 0.75f;
    public float _gravityCompensation = 0.15f;
    private bool _engaged;
    private VRCDroneApi _localDrone;
    
    void Start()
    {
        _localDrone = Networking.LocalPlayer.GetDrone();
        QuickMenu.Instance().RegisterFloat("Fishbowl/Strength", this, nameof(_strength), nameof(MenuVariableChanged), 0, 200);
        QuickMenu.Instance().RegisterFloat("Fishbowl/Lerp", this, nameof(_lerp), nameof(MenuVariableChanged), 0, 1);
        QuickMenu.Instance().RegisterFloat("Fishbowl/Threshold", this, nameof(_threshold), nameof(MenuVariableChanged), 0, 1);
        QuickMenu.Instance().RegisterFloat("Fishbowl/Dampening", this, nameof(_dampening), nameof(MenuVariableChanged), 0, 100);
        QuickMenu.Instance().RegisterFloat("Fishbowl/GravityCompensation", this, nameof(_gravityCompensation), nameof(MenuVariableChanged), 0, 1);
        QuickMenu.Instance().RegisterFloat("Fishbowl/PushStrength", this, nameof(_pushStrength), nameof(MenuVariableChanged), 0, 100);
    }
    
    public void MenuVariableChanged()
    {
        
    }
    
    public override void OnDroneTriggerEnter(VRCDroneApi drone)
    {
        if (!drone.GetPlayer().isLocal) return;
        if (_engaged) return;
        _engaged = true;
        if (Utilities.IsValid(rigidBody))
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }
        UpdateLoop();
    }

    public void UpdateLoop()
    {
        if (!_localDrone.IsDeployed()) _engaged = false;
        if (Vector3.Distance(_localDrone.GetPosition(), transform.position) > _threshold) _engaged = false;
        if (!_engaged) return;
        SendCustomEventDelayedSeconds(nameof(UpdateLoop), 0);
        _localDrone.SetVelocity(_localDrone.GetVelocity() + Vector3.up * _gravityCompensation);
        _localDrone.SetVelocity(Vector3.Lerp(_localDrone.GetVelocity(), (target.position - _localDrone.GetPosition()) * _strength * _strengthCurve.Evaluate(Vector3.Distance(_localDrone.GetPosition(), transform.position) / _threshold), _lerp * Time.deltaTime));
        _localDrone.SetVelocity(Vector3.Lerp(_localDrone.GetVelocity(), Vector3.zero, Time.deltaTime * _dampening));
        if (Utilities.IsValid(rigidBody)) rigidBody.AddForce((_localDrone.GetPosition() - rigidBody.position) * Time.deltaTime * _pushStrength);
    }
}
