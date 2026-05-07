
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;
using Random = UnityEngine.Random;

public enum GunEvent
{
    None,
    TriggerDown,
    TriggerUp,
    TryToShoot,
    Shoot,
    HammerUp,
    HammerDown,
    ActionOpen,
    ActionClose,
    ClearChamber,
    FeedChamber,
    MagOut,
    MagIn,
}

public enum ActionHoldType
{
    NoHold,
    AlwaysHold,
    EmptyMagHold,
    EmptyChamberHold,
}
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class MountedTurret : UdonSharpBehaviour
{
    private string[] GunEventStrings = new string[]
    {
        "None",
        "TriggerDown",
        "TriggerUp",
        "TryToShoot",
        "Shoot",
        "HammerUp",
        "HammerDown",
        "ActionOpen",
        "ActionClose",
        "ClearChamber",
        "FeedChamber",
        "MagOut",
        "MagIn",
    };
    public GenericPool projectilePool;
    public EventSync eventSync;
    public Transform worldTransform;
    public Transform stand;
    
    public Transform firePos;
    public ParticleSystem spentParticle;
    public ParticleSystem unspentParticle;
    public ParticleSystem fireParticle;
    public AudioSource fireSource;
    public AudioSource otherSource;
    public GunSlider actionInteract;
    public GunSlider magazineInteract;
    public GunSlider hammerInteract;
    
    public AudioClip[] fireClips;
    public AudioClip[] muffledFireClips;
    public AudioClip magOutSound;
    public AudioClip magInSound;
    public AudioClip actionOpenSound;
    public AudioClip actionCloseSound;
    public AudioClip feedChamberSound;
    public AudioClip modeSwitchSound;
    public AudioClip hammerClick;
    
    public string projectileName;
    
    public bool loadChambersSimultaneously;
    public bool unloadChambersSimultaneously;
    public bool disconnectWithoutMag;
    
    public ActionHoldType actionHoldType = ActionHoldType.EmptyMagHold;

    public GunEvent[] triggerDownEvents = new GunEvent[] { GunEvent.TryToShoot };
    public GunEvent[] tryToShootEvents = new GunEvent[] { GunEvent.HammerDown };
    public GunEvent[] triggerUpEvents = new GunEvent[]{};
    public GunEvent[] shootEvents = new GunEvent[] { GunEvent.ActionOpen };
    public GunEvent[] actionOpenEvents = new GunEvent[] { GunEvent.ClearChamber, GunEvent.HammerUp };
    public GunEvent[] actionCloseEvents = new GunEvent[] { GunEvent.FeedChamber };
    public GunEvent[] hammerDownEvents = new GunEvent[] { GunEvent.Shoot };
    public GunEvent[] hammerUpEvents = new GunEvent[] {};
    public GunEvent[] clearChamberEvents = new GunEvent[] {};
    public GunEvent[] feedChamberEvents = new GunEvent[] {};
    public GunEvent[] magOutEvents = new GunEvent[] {};
    public GunEvent[] magInEvents = new GunEvent[] {};
    
    public int numShots;
    public bool automatic;
    public float shootDelay;
    public float burstDelay;
    public float chargeDelay;
    
    public float volume = 1;
    public int magCount = 15;
    public int chamberCount = 1;


    private VRCPickup _pickup;
    [SerializeField]
    private int _ammo;
    [SerializeField]
    private int _currentShot;
    [SerializeField]
    private int _chamberUnspent = 0;
    [SerializeField]
    private int _chamberSpent = 0;
    [SerializeField]
    private bool _magazine = false;
    [SerializeField]
    private bool _hammer = false;
    [SerializeField]
    private bool _trigger = false;
    [SerializeField]
    private bool _holdAction = false;
    [SerializeField]
    private bool _actionOpen = false;

    [UdonSynced]
    private float _syncedHeight;
    [UdonSynced]
    private Quaternion _syncedRotation;

    
    private float _syncInterval;
    private float _lastSync;


    private void OnEnable()
    {
        _ammo = magCount;
        _chamberUnspent = chamberCount;
        _hammer = true;
        _magazine = true;
        _actionOpen = false;
    }

    private void Start()
    {
        _pickup = (VRCPickup)GetComponent(typeof(VRCPickup));
    }

    public override void OnPickupUseDown()
    {
        TriggerDown();
    }
    public override void OnPickupUseUp()
    {
        TriggerUp();
    }

    public void TriggerDown()
    {
        _currentShot = 1;
        _trigger = true;
        RunAssociatedEvents(GunEvent.TriggerDown);
        UpdateAnimator();
    }

    public void TriggerUp()
    {
        _trigger = false;
        RunAssociatedEvents(GunEvent.TriggerUp); 
        UpdateAnimator();
    }

    public override void PostLateUpdate()
    {
        if (!Networking.IsOwner(gameObject)) return;
        float height = UpdateStand();
        if (height != _syncedHeight && _lastSync + _syncInterval < Time.timeSinceLevelLoad)
        {
            _lastSync = Time.timeSinceLevelLoad;
            _syncedHeight = height;
            RequestSerialization();
        }
    }

    public override void OnPreSerialization()
    {
        _syncedRotation = transform.localRotation;
        _syncedHeight = transform.localPosition.y;
    }

    public override void OnDeserialization(DeserializationResult result)
    {
        transform.localPosition = new Vector3(0, _syncedHeight, 0);
        transform.localRotation = _syncedRotation;
        UpdateStand();
    }
    public float UpdateStand()
    {
        Vector3 position = transform.localPosition;
        Vector3 standScale = stand.localScale;
        Vector3 standPosition = stand.localPosition;
        position.x = 0;
        position.z = 0;
        position.y = Mathf.Clamp(position.y, 0.1f, 1.5f);
        standScale.y = position.y;
        standPosition.y = position.y / 2;
        stand.localPosition = standPosition;
        stand.localScale = standScale;
        transform.localPosition = position;
        return position.y;
    }

    public void UpdateAnimator()
    {
        
    }
    
    [RecursiveMethod]
    private void RunAssociatedEvents(GunEvent gunEvent)
    {
        if (!Networking.IsOwner(gameObject)) return;
        eventSync.SyncEvent(Convert.ToByte(gunEvent));
        GunEvent[] events;
        switch (gunEvent)
        {
            case GunEvent.TriggerDown:
                events = triggerDownEvents;
                break;
            case GunEvent.TriggerUp:
                events = triggerUpEvents;
                break;
            case GunEvent.TryToShoot:
                events = tryToShootEvents;
                break;
            case GunEvent.Shoot:
                events = shootEvents;
                break;
            case GunEvent.HammerUp:
                events = hammerUpEvents;
                break;
            case GunEvent.HammerDown:
                events = hammerDownEvents;
                break;
            case GunEvent.ActionOpen:
                events = actionOpenEvents;
                break;
            case GunEvent.ActionClose:
                events = actionCloseEvents;
                break;
            case GunEvent.ClearChamber:
                events = clearChamberEvents;
                break;
            case GunEvent.FeedChamber:
                events = feedChamberEvents;
                break;
            case GunEvent.MagOut:
                events = magOutEvents;
                break;
            case GunEvent.MagIn:
                events = magInEvents;
                break;
            default: return;
        }
        for (int i = 0; i < events.Length; i++)
        {
            RunEvent(events[i], 0, 0);
        }
    }

    private float _time;
    private float _dependencyTime;
    public void RunEvent(GunEvent gunEvent, float time, float dependencyTime)
    {
        _time = time;
        _dependencyTime = dependencyTime;
        switch (gunEvent)
        {
            case GunEvent.TriggerDown:
                RunEvent(null, nameof(TriggerDown));
                break;
            case GunEvent.TriggerUp:
                RunEvent(null, nameof(TriggerUp));
                break;
            case GunEvent.Shoot:
                RunEvent(null, nameof(Shoot));
                break;
            case GunEvent.TryToShoot:
                RunEvent(null, nameof(TryToShoot));
                break;
            case GunEvent.HammerUp:
                RunEvent(hammerInteract, nameof(HammerUp));
                break;
            case GunEvent.HammerDown:
                RunEvent(hammerInteract, nameof(HammerDown));
                break;
            case GunEvent.ActionOpen:
                RunEvent(actionInteract, nameof(ActionOpen));
                break;
            case GunEvent.ActionClose:
                RunEvent(actionInteract, nameof(ActionClose));
                break;
            case GunEvent.ClearChamber:
                RunEvent(null, nameof(ClearChamber));
                break;
            case GunEvent.FeedChamber:
                RunEvent(null, nameof(FeedChamber));
                break;
            case GunEvent.MagOut:
                RunEvent(magazineInteract, nameof(MagOut));
                break;
            case GunEvent.MagIn:
                RunEvent(magazineInteract, nameof(MagIn));
                break;
        }
    }

    private void RunEvent(GunSlider interact, string eventName)
    {
        Debug.Log(eventName);
        if (Utilities.IsValid(interact))
        {
            //Debug.Log($"Sending {eventName} to {interact.name}");
            interact.JumpTo(eventName);
        }
        else
        {
            //Debug.Log($"Sending {eventName} directly");
            SendCustomEvent(eventName);
        }
    }
    
    public void CheckAuto()
    {
        if (_trigger)
        {
            _currentShot = 1;
            TryToShoot();
        }
    }

    public bool TryToShoot()
    {
        if (!_trigger)
        {
            Debug.Log("Unable to shoot because the trigger was not down");
            return false;
        }
        if (_actionOpen)
        {
            Debug.Log("Unable to shoot because the action is open");
            return false;
        }

        if (disconnectWithoutMag && !_magazine)
        {
            Debug.Log("Unable to shoot because the magazine is out");
            return false;
        }

        if (!_hammer)
        {
            Debug.Log("Unable to shoot because the hammer is not ready");
            return false;
        }
        
        //Debug.Log("TryToShoot succeeded");

        RunAssociatedEvents(GunEvent.TryToShoot);

        /*
        _currentShot++;
        if (_currentShot <= numShots)
        {
            SendCustomEventDelayedSeconds(nameof(TryToShoot), burstDelay);
        }
        if (automatic)
        {
            SendCustomEventDelayedSeconds(nameof(CheckAuto), shootDelay);
        }*/
        return true;
    }

    public void ClearChamber()
    {
        if (_chamberSpent > 0)
        {
            if (unloadChambersSimultaneously)
            {
                spentParticle.Emit(_chamberSpent);
                _chamberSpent = 0;
            }
            else
            {
                spentParticle.Emit(1);
                _chamberSpent--;
            }
        }
        else if (_chamberUnspent > 0)
        {
            if (unloadChambersSimultaneously)
            {
                unspentParticle.Emit(_chamberUnspent);
                _chamberUnspent = 0;
            }
            else
            {
                unspentParticle.Emit(1);
                _chamberUnspent--;
            }
        }
        RunAssociatedEvents(GunEvent.ClearChamber);
        Debug.Log($"Cleared chamber, now has {_chamberUnspent} unspent, {_chamberSpent} spent");
        UpdateAnimator();
        UpdateStand();
    }

    public bool useMagToFeed = true;
    public void FeedChamber()
    {
        if (_chamberSpent + _chamberUnspent >= chamberCount) 
        {
            Debug.Log("Unable to feed chamber because it's already full");
            return; //this would cause a jam! But for now let's just ignore it and not do anything
        }
        
        if (useMagToFeed)
        {
            //load from magazine
            if (_magazine && _ammo > 0)
            {
                if (loadChambersSimultaneously)
                {
                    int difference = chamberCount - _chamberUnspent - _chamberSpent; //Find the number of remaining slots
                    difference = Mathf.Min(difference, _ammo); //Only pull what you can
                    _chamberUnspent += difference;
                    _ammo -= difference;
                }
                else
                {
                    _chamberUnspent++;
                    _ammo--;
                }
            }
        }
        else
        {
            //Load manually
            if (loadChambersSimultaneously)
            {
                _chamberUnspent = chamberCount;
            }
            else
            {
                _chamberUnspent++;
            }
        }
        
        if (Utilities.IsValid(feedChamberSound)) otherSource.PlayOneShot(feedChamberSound);
        RunAssociatedEvents(GunEvent.FeedChamber);
        Debug.Log($"Feed Chamber, now has {_chamberUnspent} unspent and {_chamberSpent} spent");
        UpdateAnimator();
        _pickup.PlayHaptics();
    }
    
    #region Interacts
    
    //Important architecture design:
    //When we change a variable like _actionOpen without triggering the corresponding ActionOpen() event, this means that it doesn't do anything immediately.
    //Instead, it will be sent to the animator and the animator gets to decide when to actually apply those changes
    //As a result, we get a nice delay between triggering an action and the action happening, without having to build tons of delay and update code into this script
    
    //Probably won't be doing any of that in this project and needs to be replaced


    public void HammerInteract()
    {
        _hammer = !_hammer;
        UpdateAnimator();
    }

    public void MagazineInteract()
    {
        _magazine = !_magazine;
        UpdateAnimator();
        MagOut();
        MagIn();
    }

    public void ChamberInteract()
    {
        Debug.Log("Chamber interact");
        if (!_actionOpen)
        {
            Debug.Log("Unable to interact with chamber because the action is not open");
            return;
        }
        if (_chamberSpent > 0)
        {
            ClearChamber();
        }
        else
        {
            FeedChamber();
        }
    }
	
    public void ActionInteract()
    {
        if (_actionOpen)
        {
            ActionClose();
        }
        else
        {
            ActionOpen();
        }
    }
    #endregion
    
	#region Animator feedback

	//Important architecture design:
	//These events are triggered by the animator to confirm the changes that have been requested by the above
	
    public void HammerDown()
    {
        _hammer = false;
        RunAssociatedEvents(GunEvent.HammerDown);
        UpdateAnimator();
    }
    public void HammerDownSoft()
    {
        _hammer = false;
        UpdateAnimator();
    }

    public void HammerUp()
    {
        _hammer = true;
        RunAssociatedEvents(GunEvent.HammerUp);
        UpdateAnimator();
    }
	public void MagOut()
    {
        if (!_magazine) return;
        //Debug.Log("Mag Out");
		if (Utilities.IsValid(magOutSound)) otherSource.PlayOneShot(magOutSound);
		_magazine = false;
		_ammo = 0;
		RunAssociatedEvents(GunEvent.MagOut);
		UpdateAnimator();
		_pickup.PlayHaptics();
	}
	
	public void MagIn()
    {
        if (_magazine) return;
        //Debug.Log("Mag In");
		if (Utilities.IsValid(magInSound)) otherSource.PlayOneShot(magInSound);
		_magazine = true;
		_ammo = magCount;
		RunAssociatedEvents(GunEvent.MagIn);
		UpdateAnimator();
		_pickup.PlayHaptics();
	}
	
	
    public void ActionOpen()
    {
        if (_actionOpen) return;
        //Debug.Log("Action opened");
        _actionOpen = true;
        
        if (actionHoldType == ActionHoldType.AlwaysHold) _holdAction = true;
        else if (actionHoldType == ActionHoldType.EmptyMagHold && _ammo < 1 && _magazine) _holdAction = true;
        else if (actionHoldType == ActionHoldType.EmptyChamberHold && _chamberUnspent + _chamberUnspent < 1) _holdAction = true;
		
        if (Utilities.IsValid(actionOpenSound)) otherSource.PlayOneShot(actionOpenSound);
        
        RunAssociatedEvents(GunEvent.ActionOpen);
		
        if (!_holdAction)
        {
            //Disable pull
        }
		
        UpdateAnimator();
        _pickup.PlayHaptics();
    }
    public void ActionClose()
    {
        if (!_actionOpen) return;
        //Debug.Log("Action closed");
        if (Utilities.IsValid(actionCloseSound)) otherSource.PlayOneShot(actionCloseSound);
        _actionOpen = false;
        _holdAction = false;
		
        RunAssociatedEvents(GunEvent.ActionClose);
        UpdateAnimator();
        _pickup.PlayHaptics();
    }
	
	#endregion

    private DataDictionary shootParameters = new DataDictionary();
    public void Shoot()
    {
        if (_chamberUnspent <= 0) //If there's no bullet to be fired, just click
        {
            Debug.Log("Unable to shoot because there is nothing in the chamber");
            _pickup.PlayHaptics();
            if (Utilities.IsValid(hammerClick)) otherSource.PlayOneShot(hammerClick);
            return;
        }
        _chamberUnspent--;
        _chamberSpent++;
        Debug.Log($"Shoot, now has {_chamberUnspent} unspent and {_chamberSpent} spent");
        RunAssociatedEvents(GunEvent.Shoot);
        UpdateAnimator();
        UpdateStand();
        _pickup.PlayHaptics();
        fireParticle.Play();
        if (Utilities.IsValid(fireSource)) fireSource.PlayOneShot(GetFireClip());
        shootParameters["PreSimulate"] = _time;
        shootParameters["IsOwner"] = Networking.IsOwner(gameObject);
        
        projectilePool.SpawnProp(projectileName, firePos.position,  Quaternion.LookRotation(firePos.forward));
        Debug.DrawRay(firePos.position, firePos.forward, Color.white, 5);
    }

    public Vector3 TransformPoint(Vector3 originPos, Quaternion originRot, Vector3 point)
    {
        point = originRot * point;
        point += originPos;
        return point;
    }

    public Quaternion TransformRotation(Quaternion originRot, Quaternion rotation)
    {
        return originRot * rotation;
    }

    private AudioClip GetFireClip()
    {
        if (fireClips.Length == 0) return null;
        int index = Random.Range(0, fireClips.Length - 1);
        return fireClips[index];
    }
}
