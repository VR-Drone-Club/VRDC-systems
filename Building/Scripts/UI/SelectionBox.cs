
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SelectionBox : UdonSharpBehaviour
{
    void Start()
    {
        
    }

    public void SetData(bool active, Vector3 start, Vector3 end)
    {
        gameObject.SetActive(active);
        Vector2 delta = end - start;
        if (delta.x < 0)
        {
            delta.x *= -1;
            start.x -= delta.x;
        }
        if (delta.y < 0)
        {
            delta.y *= -1;
            start.y -= delta.y;
        }
        
        transform.localPosition = start;
        ((RectTransform)transform).sizeDelta = delta;
    }
}
