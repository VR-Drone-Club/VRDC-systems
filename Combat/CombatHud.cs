
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Rendering;
using VRC.SDKBase;
using VRC.Udon;

public class CombatHud : UdonSharpBehaviour
{
    public Transform mover;
    public TargetingSystem targetingSystem;
    public Image leftCooldown;
    public Image rightCooldown;
    public Transform targetIndicator;
    public Animator animator;
    private DroneBalloons _droneBalloons;
    
    public override void PostLateUpdate()
    {
        if (!Networking.LocalPlayer.GetDrone().IsDeployed())
        {
            mover.gameObject.SetActive(false);
            return;
        }
        mover.gameObject.SetActive(true);
        mover.position = VRCCameraSettings.PhotoCamera.Position;
        mover.rotation = VRCCameraSettings.PhotoCamera.Rotation;
        if (!Utilities.IsValid(_droneBalloons)) _droneBalloons = DroneBalloons.Instance(Networking.LocalPlayer);
        if (_droneBalloons.balloonCount > 0) targetingSystem.Evaluate();
        if (_droneBalloons.balloonCount == 0)
        {
            mover.gameObject.SetActive(false);
            return;
        }
        leftCooldown.fillAmount = Mathf.InverseLerp(targetingSystem.fireTime, targetingSystem.fireTime + targetingSystem.cooldownTime, Time.realtimeSinceStartup) * 0.5f;
        rightCooldown.fillAmount = Mathf.InverseLerp(targetingSystem.fireTime, targetingSystem.fireTime + targetingSystem.cooldownTime, Time.realtimeSinceStartup) * 0.5f;
        if (Utilities.IsValid(targetingSystem.lastTarget))
        {
            targetIndicator.gameObject.SetActive(true);
            animator.SetFloat("FocusLevel", targetingSystem.targetingCurve.Evaluate(Mathf.InverseLerp(targetingSystem.targetAcquireTime, targetingSystem.targetAcquireTime + targetingSystem.chargeTime, Time.realtimeSinceStartup)));
            Plane plane = new Plane(targetIndicator.parent.forward, targetIndicator.parent.position);
            Ray ray = new Ray(mover.position, targetingSystem.targetPredictedPosition - mover.position);
            plane.Raycast(ray, out float enter);
            if (enter < 0)
            {
                targetIndicator.gameObject.SetActive(false);
            }
            targetIndicator.position = ray.GetPoint(enter);
        }
        else
        {
            targetIndicator.gameObject.SetActive(false);
        }
    }
}
