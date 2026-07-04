
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;


public enum EnemyShipState
{
    None,
    Patrol,
    Attack,
    Reposition,
    Retreat,
}
public class BasicFollower : AbstractActor
{
    public float patrolRange;
    public float aggression;
    public float caution;
    public float sightDistance;
    public float repositionDistance;

    [UdonSynced]
    private EnemyShipState _state;
    private Vector3 _target;
    private Vector3[] _patrolPath;
    private bool _initialized;
    private int _patrolIndex;
    
    private bool InAttackRange => Vector3.Distance(syncedPosition, Networking.LocalPlayer.GetDrone().GetPosition()) < sightDistance;
    private bool ReachedPatrolTarget => Vector3.Distance(syncedPosition, _target) < 20f;
    private bool ShouldRetreat => _health < maxHealth - aggression * maxHealth;
    private bool ShouldReposition => Vector3.Distance(syncedPosition, Networking.LocalPlayer.GetDrone().GetPosition()) < caution;
    private bool ShouldStopReposition => Vector3.Distance(syncedPosition, _target) < 10f;
    internal override void Start()
    {
        base.Start();
        if (!_initialized) Initialize();
    }

    public override void Initialize()
    {
        base.Initialize();
        _health = maxHealth;
        _initialized = true;
    }
    public override void DeserializeProp(DataDictionary parameters)
    {
        base.DeserializeProp(parameters);
        if (!_initialized) Initialize();
        _patrolPath = parameters.GetList("Path").ToVector3Array();
        SetState(EnemyShipState.Patrol);
    }

    public override void OnPreSerialization()
    {
        base.OnPreSerialization();
        _syncedHealth = _health;
    }

    internal override void Evaluate(float delta)
    {
        RotateTowardPoint(_target, delta);
        AccelerateTowardPoint(syncedPosition + syncedRotation * Vector3.forward, delta);
        SetState(EvaluateState());
        base.Evaluate(delta);
    }

    EnemyShipState EvaluateState()
    {
        switch (_state)
        {
            case EnemyShipState.None: return EvaluateNone();
            case EnemyShipState.Patrol: return EvaluatePatrol();
            case EnemyShipState.Attack: return EvaluateAttack();
            case EnemyShipState.Reposition: return EvaluateReposition();
            case EnemyShipState.Retreat: return EvaluateRetreat();
        }

        return _state;
    }

    void SetState(EnemyShipState state)
    {
        if (_state == state) return;
        //Debug.Log($"Set state to {state}");
        _state = state;
        switch (state)
        {
            case EnemyShipState.None: break;
            case EnemyShipState.Patrol:
                SetPatrol();
                break;
            case EnemyShipState.Attack:
                SetAttack();
                break;
            case EnemyShipState.Reposition:
                SetReposition();
                break;
            case EnemyShipState.Retreat:
                SetRetreat();
                break;
        }
    }

    EnemyShipState EvaluateNone()
    {
        return EnemyShipState.None;
    }

    void SetPatrol()
    {
        if (_patrolPath == null)
        {
            _target = syncedPosition + Random.insideUnitSphere * patrolRange;
        }
        else
        {
            _target = _patrolPath[_patrolIndex] + Random.insideUnitSphere * patrolRange;
            _patrolIndex = (_patrolIndex + 1) % _patrolPath.Length;
        }
    }
    EnemyShipState EvaluatePatrol()
    {
        if (ShouldRetreat) return EnemyShipState.Retreat;
        if (InAttackRange) return EnemyShipState.Attack;
        if (ReachedPatrolTarget)
        {
            _state = EnemyShipState.None;
            return EnemyShipState.Patrol;
        }

        return EnemyShipState.Patrol;
    }

    void SetAttack()
    {
        
    }
    EnemyShipState EvaluateAttack()
    {
        if (ShouldRetreat) return EnemyShipState.Retreat;
        if (ShouldReposition) return EnemyShipState.Reposition;
        _target = Networking.LocalPlayer.GetDrone().GetPosition();
        return EnemyShipState.Attack;
    }

    void SetReposition()
    {
        //_target = _position + (_rotation * Vector3.back * repositionDistance + (Random.insideUnitSphere * repositionDistance / 2));
        Vector3 position = Random.insideUnitCircle;
        position = Vector3.Scale(position, new Vector3(1, 0.1f));
        position = position.normalized * repositionDistance;
        position.z = repositionDistance;
        position = Quaternion.LookRotation(Vector3.Scale(Networking.LocalPlayer.GetDrone().GetPosition() - syncedPosition, new Vector3(1,0,1)),  _worldTransform.up) * position;
        position += Networking.LocalPlayer.GetDrone().GetPosition();
        _target = position;
    }
    EnemyShipState EvaluateReposition()
    {
        if (ShouldRetreat) return EnemyShipState.Retreat;
        if (ShouldStopReposition) return EnemyShipState.Attack;
        return EnemyShipState.Reposition;
    }

    void SetRetreat()
    {
        
    }
    EnemyShipState EvaluateRetreat()
    {
        return EnemyShipState.Retreat;
    }
}