using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDK3.Data;

public static class DataDictionaryExtensions
{
    public static bool TryGetDataList(this DataDictionary dictionary, DataToken key, out DataList value)
    {
        if (!dictionary.TryGetValue(key, out DataToken token))
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

    public static DataList GetList(this DataDictionary dictionary, DataToken key)
    {
        DataToken token = dictionary[key];
        return token.DataList;
    }

    public static bool TryGetDataDictionary(this DataDictionary dictionary, DataToken key, out DataDictionary value)
    {
        if (!dictionary.TryGetValue(key, TokenType.DataDictionary, out DataToken token))
        {
            value = default;
            return false;
        }

        value = token.DataDictionary;

        return true;
    }

    public static DataDictionary GetDictionary(this DataDictionary dictionary, DataToken key)
    {
        DataToken token = dictionary[key];
        return token.DataDictionary;
    }

    public static bool TryGetInt(this DataDictionary dictionary, DataToken key, out int value)
    {
        if (!dictionary.TryGetValue(key, out DataToken token))
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

    public static int GetInt(this DataDictionary dictionary, DataToken key)
    {
        DataToken token = dictionary[key];
        if (token.TokenType == TokenType.Int) return token.Int;
        else if (token.IsNumber) return (int)token.Number;
        return token.Int;
    }

    public static bool TryGetFloat(this DataDictionary dictionary, DataToken key, out float value)
    {
        if (!dictionary.TryGetValue(key, out DataToken token))
        {
            value = default;
            return false;
        }

        if (token.TokenType == TokenType.Float) value = token.Float;
        else if (token.IsNumber) value = (float)token.Number;
        else
        {
            value = default;
            return false;
        }

        return true;
    }

    public static float GetFloat(this DataDictionary dictionary, DataToken key)
    {
        DataToken token = dictionary[key];
        if (token.TokenType == TokenType.Float) return token.Float;
        else if (token.IsNumber) return (float)token.Number;
        return token.Int;
    }

    public static bool TryGetString(this DataDictionary dictionary, DataToken key, out string value)
    {
        if (!dictionary.TryGetValue(key, out DataToken token))
        {
            value = default;
            return false;
        }

        if (token.TokenType == TokenType.String) value = token.String;
        else
        {
            value = default;
            return false;
        }

        return true;
    }

    public static string GetString(this DataDictionary dictionary, string key)
    {
        DataToken token = dictionary[key];
        return token.String;
    }
    public static bool TryGetReference<T>(this DataDictionary list, DataToken key, out T value)
    {
        if (!list.TryGetValue(key, TokenType.Reference, out DataToken token))
        {
            value = default;
            return false;
        }

        value = (T)token.Reference;
        return true;
    }
    public static T GetReference<T>(this DataDictionary list, DataToken key)
    {
        return (T)list[key].Reference;
    }
}