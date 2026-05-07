
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Graph : ExternNode
{
    public ParticleSystem[] particleSystems;
    public Transform min;
    public Transform max;
    void Start()
    {
        inputNames = new string[particleSystems.Length];
        for (int i = 0; i < inputNames.Length; i++)
        {
            inputNames[i] = $"Input {i + 1}";
        }
    }

    public override void SetInput(int index, float value)
    {
        particleSystems[index].transform.position = Vector3.Lerp(min.position, max.position, value);
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
