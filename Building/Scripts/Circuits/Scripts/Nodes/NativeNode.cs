
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

public class NativeNode : CircuitsNode
{
    public Instruction operation;
    public Transform inputMin;
    public Transform inputMax;
    public Transform output;
    private int numInputs;
    private int numOutputs;
    private DataDictionary instructionInputs = new DataDictionary()
    {
        [Convert.ToByte(Instruction.NOT)] = new DataList {"Input"},
        [Convert.ToByte(Instruction.AND)] = new DataList {"Input", "Input"},
        [Convert.ToByte(Instruction.NAND)] = new DataList {"Input", "Input"},
        [Convert.ToByte(Instruction.OR)] = new DataList {"Input", "Input"},
        [Convert.ToByte(Instruction.NOR)] = new DataList {"Input", "Input"},
        [Convert.ToByte(Instruction.XOR)] = new DataList {"Input", "Input"},
        [Convert.ToByte(Instruction.XNOR)] = new DataList {"Input", "Input"},
        [Convert.ToByte(Instruction.Add)] = new DataList {"Input", "Input"},
        [Convert.ToByte(Instruction.Subtract)] = new DataList {"Input", "Input"},
        [Convert.ToByte(Instruction.Multiply)] = new DataList {"Input", "Input"},
        [Convert.ToByte(Instruction.Divide)] = new DataList {"Input", "Input"},
        [Convert.ToByte(Instruction.Modulo)] = new DataList {"Input", "Input"},
        [Convert.ToByte(Instruction.Repeat)] = new DataList {"Input", "Length"},
        [Convert.ToByte(Instruction.Log)] = new DataList {"Input", "Power"},
        [Convert.ToByte(Instruction.Pow)] = new DataList {"Input", "Power"},
        [Convert.ToByte(Instruction.Max)] = new DataList {"Input", "Input"},
        [Convert.ToByte(Instruction.Min)] = new DataList {"Input", "Input"},
        [Convert.ToByte(Instruction.Abs)] = new DataList {"Input"},
        [Convert.ToByte(Instruction.Cos)] = new DataList {"Input"},
        [Convert.ToByte(Instruction.Acos)] = new DataList {"Input"},
        [Convert.ToByte(Instruction.Sin)] = new DataList {"Input"},
        [Convert.ToByte(Instruction.Asin)] = new DataList {"Input"},
        [Convert.ToByte(Instruction.Tan)] = new DataList {"Input"},
        [Convert.ToByte(Instruction.Atan)] = new DataList {"Input"},
        [Convert.ToByte(Instruction.Round)] = new DataList {"Input"},
        [Convert.ToByte(Instruction.Floor)] = new DataList {"Input"},
        [Convert.ToByte(Instruction.Ceil)] = new DataList {"Input"},
        [Convert.ToByte(Instruction.Sign)] = new DataList {"Input"},
        [Convert.ToByte(Instruction.Sqrt)] = new DataList {"Input"},
        [Convert.ToByte(Instruction.Clamp)] = new DataList {"Input", "min", "max"},
        [Convert.ToByte(Instruction.InverseLerp)] = new DataList {"min", "max", "Input"},
        [Convert.ToByte(Instruction.Lerp)] = new DataList {"min", "max", "Input"},
        [Convert.ToByte(Instruction.SmoothStep)] = new DataList {"min", "max", "Input"},
        [Convert.ToByte(Instruction.MoveTowards)] = new DataList {"min", "max", "Input"},
        [Convert.ToByte(Instruction.Equal)] = new DataList {"Input", "Input"},
        [Convert.ToByte(Instruction.Greater)] = new DataList {"Input", "Input"},
        [Convert.ToByte(Instruction.GreaterEqual)] = new DataList {"Input", "Input"},
        [Convert.ToByte(Instruction.Less)] = new DataList {"Input", "Input"},
        [Convert.ToByte(Instruction.LessEqual)] = new DataList {"Input", "Input"},
    };
    
    void Start()
    {

        numOutputs = 1;
        outputNames = new string[] { "Output" };
        
        Initialize();

        if (_circuitsManager.TryGetNodeProperty(this, "NativeOp", out DataToken nativeOpToken))
        {
            operation = (Instruction)Convert.ToByte(nativeOpToken);
        }
        else
        {
            _circuitsManager.SetNodeProperty(this, "NativeOp", Convert.ToByte(operation));
        }
        OperationChanged();
    }

    private void OperationChanged()
    {
        if (!instructionInputs.TryGetValue(Convert.ToByte(operation), out DataToken token)) return;
        DataList names = token.DataList;
        numInputs = names.Count;
        inputNames = new string[names.Count];
        for (int i = 0; i < inputNames.Length; i++)
        {
            inputNames[i] = names[i].String;
        }
        SendProperties();
        _circuitsManager.RequestCompile();
    }

    public override Vector3 GetNearestWirePosition(int index, bool isInput, Vector3 other)
    {
        if (isInput)
        {
            return Vector3.Lerp(inputMin.position, inputMax.position, Mathf.InverseLerp(1, numInputs, index + 1));
        }
        else
        {
            return output.position;
        }
    }

    public override NodeType GetNodeType()
    {
        return NodeType.NativeNode;
    }

    public override bool RequireAllInputs()
    {
        return true;
    }
}
