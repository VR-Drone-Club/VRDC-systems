
using UdonSharp;
using UdonToolkit;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;


[ExecuteInEditMode]
public class RushwayPair : WorldPropTemplate
{
    public int length;
    public int interval = 300;
    public Rushway forward;
    public Rushway returning;

    public override DataDictionary SerializeProp()
    {
        if (!Utilities.IsValid(currentParameters)) currentParameters = new DataDictionary();
        SetIntParameter(nameof(length), length, 7);
        SetIntParameter(nameof(interval), interval, 300);
        return currentParameters;
    }

    public override void DeserializeProp(DataDictionary parameters)
    {
        if (!Utilities.IsValid(parameters)) return;
        currentParameters = parameters.DeepClone();
        length = GetIntParameter(nameof(length), 7);
        interval = GetIntParameter(nameof(interval), 300);
        ApplyLength();
    }
    
    public override DataList GetSpriteData()
    {
        if (!hasSprite || !Utilities.IsValid(sprite)) return null;
        DataList list = new DataList();
        list.Add(spriteOrder);
        list.Add(sprite.name);
        list.Add(new Vector3(0,0, interval * (length - 1) / 2).ToDataToken());
        list.Add(Quaternion.Euler(spriteOffsetRotation).ToDataToken());
        list.Add(new Vector3(150, 0, interval * (length - 1)).ToDataToken());
        return list;
    }

    [Button("Apply Length")]
    public void ApplyLength()
    {
        for (int i = 0; i < forward.transform.childCount; i++)
        {
            Vector3 position = forward.transform.GetChild(i).localPosition;
            position.z = interval * i;
            forward.transform.GetChild(i).localPosition = position;
            
            forward.transform.GetChild(i).gameObject.SetActive(i + 1 <= length);
        }
        forward.RegisterAllGates();
        for (int i = 0; i < returning.transform.childCount; i++)
        {
            Vector3 position = returning.transform.GetChild(i).localPosition;
            position.z = interval * i;
            returning.transform.GetChild(i).localPosition = position;
            
            int inverseIndex = returning.transform.childCount - 1 - i;
            returning.transform.GetChild(inverseIndex).gameObject.SetActive(inverseIndex + 1 <= length);
        }
        returning.RegisterAllGates();

        targetPosition = new Vector3(0, 0,(length - 1) * interval);
        Vector3 returningPosition = returning.transform.localPosition;
        returningPosition.z = interval * (length - 1);
        returning.transform.localPosition = returningPosition;
    }
    
    [SerializeField, HideInInspector]
    public Vector3 targetPosition = new Vector3(0f, 0f, 1200f);

#if UNITY_EDITOR && !COMPILER_UDONSHARP
    public void ApplyTargetPosition()
    {
        targetPosition.z = Mathf.Clamp(targetPosition.z, 0, (forward.transform.childCount - 1) * interval);
        transform.LookAt(transform.TransformPoint(targetPosition));
        float totalDistance = targetPosition.magnitude;
        length = Mathf.RoundToInt(totalDistance / interval) + 1;
        ApplyLength();
    }
    private void OnDrawGizmos()
    {
        float time = (length - 1) * interval / forward.speed;
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.black;
        Handles.Label((forward.transform.position + returning.transform.position) / 2 + new Vector3(0,200,0), $"{time} seconds", style);
    }
    #endif
}
