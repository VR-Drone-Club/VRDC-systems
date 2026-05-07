
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;


public class DataStack : DataList
{
    public static DataStack New()
    {
        #if UNITY_EDITOR && !COMPILER_UDONSHARP
        return new DataStack();
#else
        return (DataStack)new DataList();
        #endif
    }
}
public static class DataStackExtensions
{

    public static void Push(this DataStack stack, DataToken token)
    {
        stack.Add(token);
    }
    
    public static DataToken Pop(this DataStack stack)
    {
        if (!Utilities.IsValid(stack)) return new DataToken(DataError.TypeUnsupported, "Unable to pop stack: valid stack not provided");
        if (stack.Count == 0) return new DataToken(DataError.IndexOutOfRange, "Unable to pop stack: no entries left");
        DataToken result = stack[stack.Count - 1];
        stack.RemoveAt(stack.Count - 1);
        return result;
    }

    public static DataToken Peek(this DataStack stack)
    {
        if (!Utilities.IsValid(stack)) return new DataToken(DataError.TypeUnsupported, "Unable to peek stack: valid stack not provided");
        if (stack.Count == 0) return new DataToken(DataError.IndexOutOfRange, "Unable to peek stack: no entries left");
        DataToken result = stack[stack.Count - 1];
        return result;
    }
}
