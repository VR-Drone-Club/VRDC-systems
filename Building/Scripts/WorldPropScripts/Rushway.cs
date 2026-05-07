
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Rushway : UdonSharpBehaviour
{
    public float threshold = 0.5f;
    public float speed = 100f;
    public float acceleration = 1f;
    public float angularAcceleration = 100f;
    public float horizontalCenteringStrength = 5f;
    public float verticalCenteringStrength = 1f;
    public RushwayGate lastGate;
    private float _lastGateDistance;
    void Start()
    {
        
    }

    public void RegisterGate(RushwayGate gate)
    {
        float distance = Vector3.Distance(transform.position, gate.transform.position);
        if (distance > _lastGateDistance)
        {
            _lastGateDistance = distance;
            lastGate = gate;
        }
        else
        {
            Debug.Log("");
        }
    }
    public void OnShipTriggerEnter(RushwayGate gate)
    {
        /*
        if (!Utilities.IsValid(_playerShip)) _playerShip = PlayerShipFinder.FindPlayerShip();
        Vector3 shipDirection = _playerShip.transform.forward;
        Vector3 gateDirection = transform.forward;

        if (Vector3.Dot(shipDirection, gateDirection) > threshold)
        {
            _playerShip.EnterHyperLane(this);
        }*/
    }

    public bool IsInHyperLane(Vector3 position)
    {
        Vector3 localPosition = transform.InverseTransformPoint(position);
        return localPosition.z < _lastGateDistance;
    }

    public Vector3 GetVector(Vector3 position)
    {
        position = transform.InverseTransformPoint(position);
        Vector3 vector = new Vector3(-position.x * horizontalCenteringStrength, -position.y * verticalCenteringStrength, speed);
        vector = vector.normalized * speed;
        return transform.TransformVector(vector);
    }

    public void RegisterAllGates()
    {
        lastGate = null;
        _lastGateDistance = Mathf.NegativeInfinity;
        var gates = GetComponentsInChildren<RushwayGate>();
        for (int i = 0; i < gates.Length; i++)
        {
            RegisterGate(gates[i]);
        }
    }
    
#if UNITY_EDITOR && !COMPILER_UDONSHARP
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.black;
        if (!Utilities.IsValid(lastGate))
        {
            RegisterAllGates();
        }
        else
        {
            Gizmos.DrawLine(transform.position, lastGate.transform.position);
        }
        
        Gizmos.color = Color.black;
        Gizmos.DrawLine(lastGate.transform.TransformPoint(new Vector3(25,0,150)), lastGate.transform.TransformPoint(new Vector3(-25, 0, 150)));
        Gizmos.DrawLine(lastGate.transform.TransformPoint(new Vector3(-25,0,150)), lastGate.transform.TransformPoint(new Vector3(-25, 0, 0)));
        Gizmos.DrawLine(lastGate.transform.TransformPoint(new Vector3(-25,0,0)), lastGate.transform.TransformPoint(new Vector3(25, 0, 0)));
        Gizmos.DrawLine(lastGate.transform.TransformPoint(new Vector3(25,0,0)), lastGate.transform.TransformPoint(new Vector3(25, 0, 150)));
    }
    #endif
}
