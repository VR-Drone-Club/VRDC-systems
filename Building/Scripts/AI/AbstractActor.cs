
using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using WorldPropScripts;

public abstract class AbstractActor : AbstractLockstepObject
{
    internal Transform _worldTransform;

    public float maxHealth;
    public float normalDrag;
    public float angularDrag;
    public float acceleration;
    public float angularAcceleration;
    [NonSerialized] 
    public int id;
    
    [UdonSynced]
    internal float _syncedHealth;
    internal float _health;
    private bool _queueDestruction;
    private Vector3 _velocity;
    private Quaternion _angularVelocity;
    private UIPool _uiPool;
    private DamageManager _damageManager;
    private Collider _collider;

    public float Health => _health;
    public float HealthPercentage => _health / maxHealth;

    internal override void Start()
    {
        base.Start();
        _worldTransform = transform.parent;
        _damageManager = DamageManagerFinder.FindDamageManager();
        _collider = GetComponent<Collider>();
        _damageManager.RegisterDamageReceiver(_collider, maxHealth, (UdonBehaviour)(Component)this);
    }

    private void OnEnable()
    {
        if (!Utilities.IsValid(_uiPool)) _uiPool = UIManagerFinder.FindDamageManager();
        _uiPool.RegisterActor(this);
    }

    private void OnDisable()
    {
        _uiPool.UnregisterActor(this);
    }

    public override void DeserializeProp(DataDictionary parameters)
    {
        base.DeserializeProp(parameters);
        id = parameters["ID"].Int;
        _health = maxHealth;
        _queueDestruction = false;
    }

    public void HealthChanged()
    {
        _health = _damageManager.GetHealth(_collider);
        ApplyHealth();
    }

    private int _deserializationCount;
    private bool _waitForNext;

    public override void OnPreSerialization()
    {
        base.OnPreSerialization();
        if (_queueDestruction)
        {
            Debug.Log($"Owner destroying {name} {id}");
            gameObject.SetActive(false);
        }
    }

    public override void OnDeserialization(DeserializationResult result)
    {
        base.OnDeserialization(result);
        _deserializationCount++;
        //if (_deserializationCount < 5) return;
        if (_syncedHealth < _health)
        {
            //If owner says health is lower, change immediate
            _health = _syncedHealth;
            _damageManager.SetHealth(_collider, _health);
            ApplyHealth();
        }
        else if (_syncedHealth > _health)
        {
            //If owner says health is higher, wait until next serialization to be certain
            if (_waitForNext)
            {
                _health = _syncedHealth;
                _damageManager.SetHealth(_collider, _health);
                ApplyHealth();
            }
            _waitForNext = !_waitForNext;
        }
        else
        {
            _waitForNext = false;
        }
    }

    private void ApplyHealth()
    {
        if (Networking.IsOwner(gameObject))
        {
            if (_health <= 0)
            {
                Debug.Log($"Owner queueing destruction of {name} {id}");
                _queueDestruction = true;
                RequestSerialization();
            }
        }
        else
        {
            if (_syncedHealth <= 0)
            {
                Debug.Log($"Non-owner queueing destruction of {name} {id}");
                gameObject.SetActive(false);
            }
        }
        _uiPool.UpdateActorUI(this);
    }

    internal override void Evaluate(float delta)
    {
        //GetPositionAndRotation(out syncedPosition, out syncedRotation);

        //Apply drag
        _velocity *= 1 - normalDrag * delta;
        Quaternion oldAngularVelocity = _angularVelocity; 
        _angularVelocity = Quaternion.Slerp( Quaternion.identity, _angularVelocity,  1 - angularDrag * delta);
        /*
        if (oldAngularVelocity != Quaternion.identity && _angularVelocity == Quaternion.identity)
        {
            Debug.Log("Angular drag might be too high");
        }*/
        
        DrawRay(syncedPosition, syncedRotation * (Vector3.forward * 20), Color.white, delta);
        DrawRay(syncedPosition, syncedRotation * Vector3.forward * _angularVelocity.z, Color.blue, delta);
        DrawRay(syncedPosition, syncedRotation * Vector3.up * _angularVelocity.y, Color.green, delta);
        DrawRay(syncedPosition, syncedRotation * Vector3.right * _angularVelocity.x, Color.red, delta);
        
        //Apply velocity
        syncedPosition += _velocity * delta;
        syncedRotation *= Quaternion.Slerp(Quaternion.identity, _angularVelocity,  delta);
        //Debug.Log($"Evaluated angular velocity {_angularVelocity.eulerAngles} to rotation {syncedRotation.eulerAngles}");
        //SetPositionAndRotation(syncedPosition, syncedRotation);
    }
    
    internal void AccelerateTowardPoint(Vector3 vector, float delta)
    {
        _velocity += (vector - syncedPosition).normalized * acceleration * delta;
    }

    internal void RotateTowardPoint(Vector3 point, float delta)
    {
        DrawLine(syncedPosition, point, Color.green, delta);
        point = InverseTransformPoint(syncedPosition, syncedRotation, point);
        Quaternion deltaQuat = Quaternion.LookRotation(point, Quaternion.Inverse(syncedRotation) * _worldTransform.up);
        deltaQuat = Quaternion.RotateTowards(Quaternion.identity, deltaQuat, angularAcceleration * delta);
        
        DrawRay(syncedPosition, syncedRotation * deltaQuat * Vector3.forward * 20, Color.red, delta);
        //Debug.Log($"adding delta {deltaQuat.eulerAngles} to angular vel {_angularVelocity.eulerAngles}");
        _angularVelocity *= deltaQuat;
        //Debug.Log($"Angular velocity is now {_angularVelocity.eulerAngles}");
    }

    public Vector3 InverseTransformPoint(Vector3 originPos, Quaternion originRot, Vector3 point)
    {
        point -= originPos;
        point = Quaternion.Inverse(originRot) * point;
        return point;
    }

    internal void DrawRay(Vector3 position, Vector3 direction, Color color, float delta)
    {
        Debug.DrawRay(_worldTransform.TransformPoint(position), _worldTransform.TransformVector(direction), color, delta);
    }

    internal void DrawLine(Vector3 position, Vector3 direction, Color color, float delta)
    {
        Debug.DrawLine(_worldTransform.TransformPoint(position), _worldTransform.TransformPoint(direction), color, delta);
    }
}
