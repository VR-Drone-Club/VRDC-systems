
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DroneMissile : DronePickup
{
    public Transform target;

    public bool position;
    public bool velocity;
    public bool acceleration;
    public bool orientation;
    
    private void FixedUpdate()
    {
        if (!Utilities.IsValid(_rigidbody)) _rigidbody = GetComponent<Rigidbody>();
        if (position) ApplyPosition(target.position);
        if (velocity) ApplyVelocity(target.position);
        if (acceleration) ApplyAcceleration(target.position);
        if (orientation) ApplyOrientation(target.position);
    }

    public override void OnAttached()
    {
        
    }
    
    public void HeldLoop()
    {
        
    }

    private Rigidbody _rigidbody;

    public float positionStrength = 1;
    private void ApplyPosition(Vector3 goal)
    {
        Vector3 difference = goal - _rigidbody.position;
        ApplyVelocity(difference * positionStrength);
    }
    public float velocityStrength = 1;
    private void ApplyVelocity(Vector3 goal)
    {
        Vector3 difference = goal - _rigidbody.velocity;
        ApplyAcceleration(difference * velocityStrength);
    }

    public float accelerationStrength;
    private void ApplyAcceleration(Vector3 goal)
    {
        Vector3 difference = goal - _rigidbody.velocity;
        ApplyOrientation(difference.normalized);
        ApplyThrust(difference.magnitude * accelerationStrength);
    }
    private void ApplyOrientation(Vector3 goal)
    {
        ApplyRotation(Quaternion.LookRotation(goal, _rigidbody.rotation * Vector3.forward) * Quaternion.Euler(-90, 0, 180));
    }
    
    private void ApplyRotation(Quaternion goal)
    {
        Quaternion difference = goal * Quaternion.Inverse(_rigidbody.rotation);
        difference.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180) angle -= 360;
        Vector3 angularVelocityDifference = axis * angle * Mathf.Deg2Rad;
        ApplyAngularVelocity(angularVelocityDifference);
    }

    public float thrustStrength = 1;
    private void ApplyThrust(float amount)
    {
        _rigidbody.AddForce(_rigidbody.transform.up * amount * thrustStrength);
    }

    public float angularVelocityStrength = 1;
    private void ApplyAngularVelocity(Vector3 goal)
    {
        Vector3 difference = goal - _rigidbody.angularVelocity;
        ApplyTorque(difference * angularVelocityStrength);
    }
    public float torqueStrength = 1;
    private void ApplyTorque(Vector3 goal)
    {
        _rigidbody.AddTorque(goal * torqueStrength);
    }
}
