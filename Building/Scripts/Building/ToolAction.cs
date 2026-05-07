using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

namespace VRDC_systems.Building.Scripts.Building
{
    public class ToolAction : DataDictionary
    {
        public static ToolAction Create(string name, Sprite icon, Observable available)
        {
#if UNITY_EDITOR && !COMPILER_UDONSHARP
            ToolAction toolAction = new ToolAction(); // in regular C# we just create the class directly
#else
        ToolAction toolAction = (ToolAction)new DataDictionary(); // In U# we create a DataList and cast it to the class
#endif
            toolAction.Add("name", name);
            toolAction.Add("icon", icon);
            toolAction.Add("available", available);
            toolAction.Add("subscribers", new DataList());
            return toolAction;
        }
    }

    public static class ToolActionExtensions
    {
        public static ToolAction AsToolAction(this DataDictionary dataList)
        {
            return (ToolAction)dataList;
        }
        public static ToolAction AsObservable(this DataToken dataToken)
        {
            return (ToolAction)dataToken.DataDictionary;
        }
        
        public static string Name(this ToolAction toolAction)
        {
            return toolAction["name"].String;
        }

        public static Sprite Icon(this ToolAction toolAction)
        {
            return (Sprite)toolAction["icon"].Reference;
        }
        public static ToolAction Subscribe(this ToolAction toolAction, UdonSharpBehaviour behaviour, string eventName = null, string variableName = null)
        {
            DataList subscriber = new DataList();
            subscriber.Add(behaviour);
            subscriber.Add(eventName);
            subscriber.Add(variableName);
            toolAction["subscribers"].DataList.Add(subscriber);
            return toolAction;
        }
        public static void InformSubscribers(this ToolAction toolAction)
        {
            DataList subscribers = toolAction["subscribers"].DataList;
            for (int i = 0; i < subscribers.Count; i++)
            {
                DataList subscriber = subscribers[i].DataList;
                if (subscriber[0].IsNull) continue;
                UdonBehaviour behaviour = (UdonBehaviour)subscriber[0].Reference;
                if (!subscriber[2].IsNull) behaviour.SetProgramVariable(subscriber[2].String, toolAction);
                if (!subscriber[1].IsNull) behaviour.SendCustomEvent(subscriber[1].String);
            }
        }
    }
}