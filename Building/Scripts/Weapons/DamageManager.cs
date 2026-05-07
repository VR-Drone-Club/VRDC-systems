
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;
public static class DamageManagerFinder
{
    public static DamageManager FindDamageManager()
    {
        GameObject gameObject = GameObject.Find("DamageManager");
        if (!Utilities.IsValid(gameObject)) return null;
        return gameObject.GetComponent<DamageManager>();
    }
}
public class DamageManager : UdonSharpBehaviour
{
    private DataDictionary _receivers = new DataDictionary();
    
    public void RegisterDamageReceiver(Collider collider, float maxHealth, UdonBehaviour udonBehaviour = null)
    {
        DataDictionary newReceiver = new DataDictionary();
        newReceiver["MaxHealth"] = maxHealth;
        newReceiver["Health"] = maxHealth;
        if (udonBehaviour != null)
            newReceiver["UdonBehaviour"] = udonBehaviour;
        else
            newReceiver["UdonBehaviour"] = collider.GetComponent(typeof(UdonBehaviour));
        _receivers[collider] = newReceiver;
    }

    public bool ApplyDamage(Collider collider, float damage)
    {
        if (!_receivers.TryGetDataDictionary(collider, out DataDictionary receiver))
        {
            Debug.Log($"{collider} is not a damage receiver");
            return false;
        }

        float health = receiver["Health"].Float;
        health -= damage;
        receiver["Health"] = health;
        SendEvent(receiver, "HealthChanged");
        return true;
    }

    public float GetHealth(Collider collider)
    {
        if (!Utilities.IsValid(collider) || !_receivers.TryGetDataDictionary(collider, out DataDictionary receiver))
        {
            Debug.Log($"{collider} is not a damage receiver");
            return 0;
        }
        return receiver["Health"].Float;
    }
    
    public void SetHealth(Collider collider, float value)
    {
        if (!Utilities.IsValid(collider) || !_receivers.TryGetDataDictionary(collider, out DataDictionary receiver))
        {
            Debug.Log($"{collider} is not a damage receiver");
            return;
        }

        receiver["Health"] = value;
    }

    private void SendEvent(DataDictionary receiver, string eventName)
    {
        receiver["UdonBehaviour"].GetReference<UdonBehaviour>().SendCustomEvent(eventName);
    }
}
