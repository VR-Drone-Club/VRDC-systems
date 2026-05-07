using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDK3.Data;

public static class DataTokenExtensions
{
    public static DataToken ToDataToken(this Vector3 vector)
    {
        DataList list = new DataList();
        list.Add(vector.x);
        list.Add(vector.y);
        list.Add(vector.z);
        return list;
    }
    public static DataToken ToDataToken(this Color color)
    {
        DataList list = new DataList();
        list.Add(color.r);
        list.Add(color.g);
        list.Add(color.b);
        list.Add(color.a);
        return list;
    }

    public static Vector3 ToVector3(this DataToken token)
    {
        DataList list = token.DataList;
        return new Vector3((float)list[0].Number, (float)list[1].Number, (float)list[2].Number);
    }
    public static Color ToColor(this DataToken token)
    {
        DataList list = token.DataList;
        return new Color((float)list[0].Number, (float)list[1].Number, (float)list[2].Number, (float)list[3].Number);
    }
    
    public static DataToken ToDataToken(this Quaternion rotation)
    {
        Vector3 vector = rotation.eulerAngles;
        if (Mathf.Approximately(vector.x, 0) && Mathf.Approximately(vector.z, 0))
        {
            return vector.y;
        }
        DataList list = new DataList();
        list.Add(vector.x);
        list.Add(vector.y);
        list.Add(vector.z);
        return list;
    }

    public static Quaternion ToQuaternion(this DataToken token)
    {
        if (token.TokenType == TokenType.DataList)
        {
            DataList list = token.DataList;
            return Quaternion.Euler(new Vector3((float)list[0].Number, (float)list[1].Number, (float)list[2].Number));
        }
        else if (token.IsNumber)
        {
            return Quaternion.Euler(new Vector3(0, (float)token.Number, 0));
        }
        return Quaternion.identity;
    }

    public static T GetReference<T>(this DataToken token)
    {
        return (T)token.Reference;
    }
}
