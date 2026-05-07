using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

public static class UIManagerFinder
{
    public static UIPool FindDamageManager()
    {
        GameObject gameObject = GameObject.Find("UIPool");
        if (!Utilities.IsValid(gameObject)) return null;
        return gameObject.GetComponent<UIPool>();
    }
}
public class UIPool : GenericPool
{
    public float uiScaleMultiplier = 1;
    public float maxUiScale;
    public float minUiScale;
    private DataDictionary _actors = new DataDictionary();

    public void RegisterActor(AbstractActor actor)
    {
        _actors[actor] = SpawnProp("ActorUI", Vector3.zero, Quaternion.identity);
        UpdateActorUI(actor);
    }

    public void UnregisterActor(AbstractActor actor)
    {
        _actors.Remove(actor, out DataToken token);
        GameObject actorUI = token.GetReference<GameObject>();
        ReturnProp(actorUI);
    }
    public void UpdateActorUI(AbstractActor actor)
    {
        if (!_actors.TryGetReference(actor, out GameObject actorUI))
        {
            Debug.Log($"{actor} does not have a registered actor UI");
            return;
        }

        Slider healthSlider = actorUI.GetComponentInChildren<Slider>();
        healthSlider.value = actor.HealthPercentage;
    }

    public override void PostLateUpdate()
    {
        Vector3 headPosition = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
        var keys = _actors.GetKeys();
        for (int i = 0; i < _actors.Count; i++)
        {
            AbstractActor actor = keys[i].GetReference<AbstractActor>();
            GameObject actorUI = _actors[keys[i]].GetReference<GameObject>();
            Vector3 position = actor.transform.position;
            actorUI.transform.position = position;
            actorUI.transform.rotation = Quaternion.LookRotation(position - headPosition);
            actorUI.transform.localScale = Vector3.one * Mathf.Clamp(Vector3.Distance(headPosition, position) * uiScaleMultiplier,   minUiScale, maxUiScale);
        }
    }
}