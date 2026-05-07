using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDK3.Data;

public static class DataListExtensions
{
        
    public static bool TryGetDataList(this DataList list, int index, out DataList value)
    {
        if (!list.TryGetValue(index, out DataToken token))
        {
            value = default;
            return false;
        }

        if (token.TokenType == TokenType.DataList) value = token.DataList;
        else
        {
            value = default;
            return false;
        }
        return true;
    }

    public static DataList GetList(this DataList list, int index)
    {
        DataToken token = list[index];
        return token.DataList;
    }
    
    public static bool TryGetDataDictionary(this DataList list, int index, out DataDictionary value)
    {
        if (!list.TryGetValue(index, out DataToken token))
        {
            value = default;
            return false;
        }

        if (token.TokenType == TokenType.DataDictionary) value = token.DataDictionary;
        else
        {
            value = default;
            return false;
        }
        return true;
    }

    public static DataDictionary GetDictionary(this DataList list, int index)
    {
        DataToken token = list[index];
        return token.DataDictionary;
    }
    
    public static bool TryGetInt(this DataList list, int index, out int value)
    {
        if (!list.TryGetValue(index, out DataToken token))
        {
            value = default;
            return false;
        }

        if (token.TokenType == TokenType.Int) value = token.Int;
        else if (token.IsNumber) value = (int)token.Number;
        else
        {
            value = default;
            return false;
        }
        return true;
    }

    public static int GetInt(this DataList list, int index)
    {
        DataToken token = list[index];
        if (token.TokenType == TokenType.Int) return token.Int;
        else if (token.IsNumber) return (int)token.Number;
        return token.Int;
    }
    
    public static bool TryGetString(this DataList list, int index, out string value)
    {
        if (!list.TryGetValue(index, TokenType.String, out DataToken token))
        {
            value = default;
            return false;
        }

        value = token.String;
        return true;
    }
    public static string GetString(this DataList list, int index)
    {
        return list[index].String;
    }
    
    public static bool TryGetReference<T>(this DataList list, int index, out T value)
    {
        if (!list.TryGetValue(index, TokenType.Reference, out DataToken token))
        {
            value = default;
            return false;
        }

        value = (T)token.Reference;
        return true;
    }
    public static T GetValue<T>(this DataList list, int index)
    {
        return (T)list[index].Reference;
    }

    public static Vector3[] ToVector3Array(this DataList list)
    {
        var array = new Vector3[list.Count];
        for (int i = 0; i < list.Count; i++)
        {
            array[i] = list[i].ToVector3();
        }
        return array;
    }
}