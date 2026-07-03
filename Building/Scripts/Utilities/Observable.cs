using System;
using System.Collections;
using System.Collections.Generic;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

public class Observable : DataList
{
    public static Observable Create(Array array)
    {
        return Create(new DataToken(array));
    }
    public static Observable Create(DataToken value)
    {
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        Observable observable = new Observable(); // in regular C# we just create the class directly
#else
        Observable observable = (Observable)new DataList(); // In U# we create a DataList and cast it to the class
#endif
        observable.Add(value);
        observable.Add(new DataList());
        return observable;
    }
}

public static class ObservableExtensions
{
    public static Observable AsObservable(this DataList dataList)
    {
        return (Observable)dataList;
    }
    public static Observable AsObservable(this DataToken dataToken)
    {
        return (Observable)dataToken.DataList;
    }
    public static void SetValue(this Observable observable, DataToken value)
    {
        //if (observable[0] == value) return;
        observable[0] = value;
        observable.InformSubscribers();
    }
    public static void SetValue(this Observable observable, Array value)
    {
        //if (observable[0] == value) return;
        observable[0] = new DataToken(value);
        observable.InformSubscribers();
    }

    public static Array GetArray(this Observable observable)
    {
        return (Array)observable[0].Reference;
    }

    public static DataToken GetToken(this Observable observable)
    {
        return observable[0];
    }
    public static float GetFloat(this Observable observable)
    {
        return observable[0].Float;
    }
    public static int GetInt(this Observable observable)
    {
        return observable[0].Int;
    }
    public static bool GetBool(this Observable observable)
    {
        return observable[0].Boolean;
    }
    public static string GetString(this Observable observable)
    {
        return observable[0].ToString();
    }
    public static Vector3 GetVector3(this Observable observable)
    {
        return observable[0].ToVector3();
    }
    public static DataList GetList(this Observable observable)
    {
        return observable[0].DataList;
    }
    public static DataDictionary GetDictionary(this Observable observable)
    {
        return observable[0].DataDictionary;
    }
    public static object GetReference(this Observable observable)
    {
        return observable[0].Reference;
    }
    public static bool IsValueValid(this Observable observable)
    {
        return !observable[0].IsEmpty;
    }

    public static void Subscribe(this Observable observable, UdonSharpBehaviour behaviour, string eventName = null, string variableName = null)
    {
        DataList subscriber = new DataList();
        subscriber.Add(behaviour);
        subscriber.Add(eventName);
        subscriber.Add(variableName);
        observable[1].DataList.Add(subscriber);
        
        //if (!string.IsNullOrEmpty(variableName)) behaviour.SetProgramVariable(variableName, observable);
        //if (!string.IsNullOrEmpty(eventName)) behaviour.SendCustomEvent(eventName);
    }
    public static void Subscribe(this Observable observable, UdonBehaviour behaviour, string eventName = null, string variableName = null)
    {
        DataList subscriber = new DataList();
        subscriber.Add(behaviour);
        subscriber.Add(eventName);
        subscriber.Add(variableName);
        observable[1].DataList.Add(subscriber);
        
        if (!string.IsNullOrEmpty(variableName)) behaviour.SetProgramVariable(variableName, observable);
        if (!string.IsNullOrEmpty(eventName)) behaviour.SendCustomEvent(eventName);
    }

    public static void InformSubscribers(this Observable observable)
    {
        DataList subscribers = observable[1].DataList;
        for (int i = 0; i < subscribers.Count; i++)
        {
            DataList subscriber = subscribers[i].DataList;
            if (subscriber[0].IsNull) continue;
            UdonBehaviour behaviour = (UdonBehaviour)subscriber[0].Reference;
            if (!subscriber[2].IsNull) behaviour.SetProgramVariable(subscriber[2].String, observable);
            if (!subscriber[1].IsNull) behaviour.SendCustomEvent(subscriber[1].String);
        }
    }
}