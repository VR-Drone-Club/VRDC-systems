
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

public static class ActorPoolFinder
{
    public static ActorPool FindActorPool()
    {
        GameObject gameObject = GameObject.Find("ActorPool");
        if (!Utilities.IsValid(gameObject)) return null;
        return gameObject.GetComponent<ActorPool>();
    }
}
public class ActorPool : GenericPool
{
    private int _actorCount;
    public void SpawnActor(string actorName, Vector3 position, Quaternion rotation, DataDictionary parameters)
    {
#if !COMPILER_UDONSHARP
        if (!Application.isPlaying) return; //Don't spawn actors outside play mode
#endif
        parameters["ID"] = _actorCount;
        _actorCount++;
        SpawnProp(actorName, position, rotation, parameters);
    }

    public void ClearActors()
    {
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        if (Application.isPlaying) return;
#endif
        ResetPools();
        _actorCount = 0;
    }
}