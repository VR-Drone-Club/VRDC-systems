
using Phasedragon.AdminUtilities;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DroneLapSitting : UdonSharpBehaviour
{
    public AnimationCurve skinPush;
    public AnimationCurve skinDamp;
    public float pushMultiplier;
    public float dampMultiplier;
    public float radiusMultiplier;
    private VRCDroneApi _localDrone;
    private VRCPlayerApi _currentLap;
    private VRCPlayerApi[] _allPlayers;
    private VRCPlayerApi _nearestPlayer;
    void Start()
    {
        _localDrone = Networking.LocalPlayer.GetDrone();
        QuickMenu.Instance().RegisterFloat("LapSitting/PushMultiplier", this, nameof(pushMultiplier), string.Empty, 0, 500);
        QuickMenu.Instance().RegisterFloat("LapSitting/DampMultiplier", this, nameof(dampMultiplier), string.Empty, 0, 500);
        QuickMenu.Instance().RegisterFloat("LapSitting/RadiusMultiplier", this, nameof(radiusMultiplier), string.Empty, 0, 1);
        Loop();
    }

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        UpdatePlayers();
    }

    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        UpdatePlayers();
    }

    private void UpdatePlayers()
    {
        _allPlayers = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
        VRCPlayerApi.GetPlayers(_allPlayers);
    }
    public void Loop()
    {
        if (!_localDrone.IsDeployed() || _allPlayers == null)
        {
            SendCustomEventDelayedSeconds(nameof(Loop), 5);
            return;
        }

        _nearestPlayer = null;
        float nearestDistance = float.MaxValue;
        for (int i = 0; i < _allPlayers.Length; i++)
        {
            if (!Utilities.IsValid(_allPlayers[i])) continue;
            float distance = Vector3.Distance(_allPlayers[i].GetPosition(), _localDrone.GetPosition());
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                _nearestPlayer = _allPlayers[i];
            }
        }

        if (nearestDistance > 5)
        {
            SendCustomEventDelayedSeconds(nameof(Loop), 5);
            return;
        }
        
        SendCustomEventDelayedSeconds(nameof(Loop), 1);
    }

    private void FixedUpdate()
    {
        if (!Utilities.IsValid(_nearestPlayer)) return;
        Vector3 position = _localDrone.GetPosition();
        EvaluateBone(position, _nearestPlayer.GetBonePosition(HumanBodyBones.LeftUpperLeg), _nearestPlayer.GetBonePosition(HumanBodyBones.LeftLowerLeg), 0.4f);
        EvaluateBone(position, _nearestPlayer.GetBonePosition(HumanBodyBones.LeftLowerLeg), _nearestPlayer.GetBonePosition(HumanBodyBones.LeftFoot), 0.4f);
        EvaluateBone(position, _nearestPlayer.GetBonePosition(HumanBodyBones.RightUpperLeg), _nearestPlayer.GetBonePosition(HumanBodyBones.RightLowerLeg), 0.4f);
        EvaluateBone(position, _nearestPlayer.GetBonePosition(HumanBodyBones.RightLowerLeg), _nearestPlayer.GetBonePosition(HumanBodyBones.RightFoot), 0.4f);
        EvaluateBone(position, _nearestPlayer.GetBonePosition(HumanBodyBones.Hips), _nearestPlayer.GetBonePosition(HumanBodyBones.UpperChest), 0.5f);
    }

    private void EvaluateBone(Vector3 point, Vector3 a, Vector3 b, float radius)
    {
        radius *= radiusMultiplier;
        float distance = FindDistanceToSegment(point, a, b, out Vector3 closest);
        if (distance > radius) return;
        distance /= radius;
        float push = skinPush.Evaluate(distance) * pushMultiplier;
        float damp = skinDamp.Evaluate(distance) * dampMultiplier;
        Vector3 velocity = _localDrone.GetVelocity();
        Vector3 direction = _localDrone.GetPosition() - closest;
        velocity += direction * push * Time.deltaTime;
        velocity -= velocity * damp * Time.deltaTime;
        _localDrone.SetVelocity(velocity);
    }
    
    private float FindDistanceToSegment(Vector3 P, Vector3 A, Vector3 B, out Vector3 X)
    {
        if( Vector3.Dot(A-B,P-A) > 0 ) X = A;
        else if( Vector3.Dot(B-A,P-B) > 0 ) X = B;
        else X = A + Vector3.Project( P-A , B-A );
        return Vector3.Distance(P, X);
    }
}
