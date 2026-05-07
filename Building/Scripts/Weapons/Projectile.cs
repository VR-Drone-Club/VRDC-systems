
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

public class Projectile : WorldPropTemplate
{
    public LayerMask layers;
    public GenericPool projectilePool;
    public DamageManager damageManager;
    public string hitEffect;
    public float radius;
    public AnimationCurve flightSpeed;
    public float flightTime;
    public float hitDamage;
    public float explosionRange;
    public float explosionDamage;
    public AnimationCurve dropSpeed;
    
    private float _elapsedTime;
    private float _preSimulate;
    private Vector3 _inheritedVelocity;


    public override void DeserializeProp(DataDictionary parameters)
    {
        //_inheritedVelocity = parameters["Velocity"].ToVector3().normalized * 10;
        //_inheritedVelocity = Vector3.zero;
        _preSimulate = parameters["PreSimulate"].Float;
        _elapsedTime = 0;
        Debug.Log($"Spawned bullet with preSimulate {_preSimulate} and inherited velocity {_inheritedVelocity} {_inheritedVelocity.magnitude}");
    }

    private void Update()
    {
        Simulate(Time.deltaTime);
    }

    private void Simulate(float delta)
    {
        if (_elapsedTime > flightTime)
        {
            projectilePool.ReturnProp(gameObject);
            return;
        }
        if (_preSimulate < 0)
        {
            delta += Mathf.Clamp(-_preSimulate, 0f, 0.05f);
            _preSimulate += 0.05f;
        }
        Vector3 oldPosition = transform.position;
        //Debug.Log($"Simulating projectile with {delta} delta and {-_inheritedVelocity} inherited velocity");
        transform.localPosition += (transform.localRotation * Vector3.forward * flightSpeed.Evaluate(_elapsedTime) + _inheritedVelocity) * delta;
        transform.localPosition += (Vector3.up * dropSpeed.Evaluate(_elapsedTime) * delta);
        _elapsedTime += delta;
        Vector3 newPosition = transform.position;

        Debug.DrawLine(oldPosition, newPosition);
        if (Physics.SphereCast(oldPosition, radius, newPosition - oldPosition, out RaycastHit hitInfo, Vector3.Distance(oldPosition, newPosition), layers))
        {
            if (_preSimulate < 0)
            {
                Debug.Log($"Projectile hit with {_preSimulate} preSim left!");
            }
            transform.position = hitInfo.point;
            projectilePool.ReturnProp(gameObject);
            projectilePool.SpawnProp(hitEffect, transform.localPosition, transform.localRotation);
            DoExplosion();
            damageManager.ApplyDamage(hitInfo.collider, hitDamage);
        }
    }

    private Collider[] colliders = new Collider[10];
    private void DoExplosion()
    {
        if (explosionRange == 0) return;
        int count = Physics.OverlapSphereNonAlloc(transform.position, explosionRange, colliders, layers);
        for (int i = 0; i < count && i < colliders.Length; i++)
        {
            if (!Utilities.IsValid(colliders[i])) continue;
            damageManager.ApplyDamage(colliders[i], explosionDamage);
        }
    }
}
