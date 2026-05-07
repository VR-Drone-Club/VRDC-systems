
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Door : ExternNode
{
    public GameObject door;
    private void Start()
    {
        outputNames = new string[0];
        inputNames = new string[]
        {
            "Door state"
        };
        Initialize();
        SendProperties();
    }

    public override void SetInput(int index, float value)
    {
        door.SetActive(value > 0);
    }

    public override float GetOutput(int index)
    {
        return 0;
    }

    public override bool RequireAllInputs()
    {
        return false;
    }
}
