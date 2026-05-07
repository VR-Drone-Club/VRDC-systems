
using System;
using System.Diagnostics;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using Debug = UnityEngine.Debug;

public enum Instruction
{
    Push,
    ExternInput,
    ExternOutput,
    Return,
    
    //Binary binary operations
    NOT,
    AND,
    NAND,
    OR,
    NOR,
    XOR,
    XNOR,
        
    //Binary analog operations
    Add,
    Subtract,
    Multiply,
    Divide,
    Modulo,
    Repeat,
    Log,
    Pow,
    Max,
    Min,
    
    //Unary operations
    Abs,
    Cos,
    Acos,
    Sin,
    Asin,
    Tan,
    Atan,
    Round,
    Floor,
    Ceil,
    Sign,
    Sqrt,
        
    //range operations
    Clamp,
    InverseLerp,
    Lerp,
    SmoothStep,
    MoveTowards,
        
    //comparison operations
    Equal,
    NotEqual,
    Greater,
    GreaterEqual,
    Less,
    LessEqual,
}
public class CircuitsManager : UdonSharpBehaviour
{
    private string[] _operationNames = new string[]
    {
        "Push",
        "ExternInput",
        "ExternOutput",
        "Return",

        //binary operation
        "NOT",
        "AND",
        "NAND",
        "OR",
        "NOR",
        "XOR",
        "XNOR",

        //analog operation
        "Add",
        "Subtract",
        "Multiply",
        "Divide",
        "Modulo",
        "Repeat",
        "Log",
        "Pow",
        "Max",
        "Min",

        //singular operation
        "Abs",
        "Cos",
        "Acos",
        "Sin",
        "Asin",
        "Tan",
        "Atan",
        "Round",
        "Floor",
        "Ceil",
        "Sign",
        "Sqrt",

        //range operation
        "Clamp",
        "InverseLerp",
        "Lerp",
        "SmoothStep",
        "MoveTowards",

        //comparison operation
        "Equal",
        "NotEqual",
        "Greater",
        "GreaterEqual",
        "Less",
        "LessEqual",
    };

    public void Export()
    {
        DataDictionary export = new DataDictionary();
        export["Nodes"] = _nodeProperties;
        export["Wires"] = _wireProperties;
        VRCJson.TrySerializeToJson(export, JsonExportType.Beautify, out DataToken output);
        Debug.Log(output);
    }
    
    #region Compiler

    private DataList _operations = new DataList();
    private DataList _heap = new DataList();
    
    private int _currentEntryPoint;
    private bool _needsCompile;
    private DataDictionary _nodeHeapAddresses = new DataDictionary();
    private DataDictionary _nodeEntryPoints = new DataDictionary();


    public void RequestCompile()
    {
        _needsCompile = true;
    }
    
    public void Compile()
    {
        _operations.Clear();
        _heap.Clear();
        _nodeHeapAddresses.Clear();
        _nodeEntryPoints.Clear();
        DataList nodeKeys = _nodeProperties.GetKeys();
        for (int i = 0; i < _nodeProperties.Count; i++)
        {
            DataDictionary node = _nodeProperties[nodeKeys[i]].DataDictionary;
            if (node["Template"] == "Native") continue;
            if (node["InputNodes"].DataDictionary.Count == 0) continue;
            _currentEntryPoint = _operations.Count;
            CompileNode(node, nodeKeys[i].String, true);
        }
        CompileInstruction(Instruction.Return);
        InstructionDump();
    }

    private void CompilePushHeapValue(DataToken value)
    {
        int heapAddress = _heap.IndexOf(value);
        if (heapAddress == -1)
        {
            _heap.Add(value);
            heapAddress = _heap.Count - 1;
        }
        CompileInstruction(Instruction.Push);
        _operations.Add(heapAddress);
    }

    private void CompileExternInput(string nodeHash, int index)
    {
        CompilePushHeapValue(index);
        CompilePushHeapValue(_nodeHashToObject[nodeHash]);
        CompileInstruction(Instruction.ExternInput);
    }
    
    private void CompileExternOutput(string nodeHash, int index)
    {
        _heap.Add(0f);
        int address = AssignExternHeapAddress(nodeHash, index);
        CompileInstruction(Instruction.Push);
        _operations.Add(address);
    }

    private void CompileNativeDirect(DataDictionary node)
    {
        _operations.Add(Convert.ToByte(node["Operation"].Double));
    }

    private void CompileInstruction(Instruction instruction)
    {
        _operations.Add(Convert.ToByte(instruction));
    }

    [RecursiveMethod] 
    private void CompileNode(DataDictionary node, string nodeHash, bool topLevel)
    {
        Debug.Log($"Compiling {nodeHash}");
        bool isNative = node["Template"] == "Native";

        if (isNative || topLevel)
        {
            //Compile native node
            DataDictionary inputNodes = node["InputNodes"].DataDictionary;
            int numInputs = Convert.ToInt32(node["NumInputs"].Number);
            for (int i = numInputs - 1; i >= 0; i--) //Compile dependencies for this node
            {
                if (inputNodes.ContainsKey(i.ToString()))
                {
                    //Compile prerequisites to this node
                    DataToken childNodeHash = inputNodes[i.ToString()];
                    CompileNode(_nodeProperties[childNodeHash].DataDictionary, childNodeHash.String, false); 
                }
                else
                {
                    //If there is no connection, compile a default value
                    CompilePushHeapValue(0f);
                }
            }
        }
        if (isNative)
        {
            CompileNativeDirect(node);
        }
        else
        {
            //Compile extern outputs
            DataDictionary outputNodes = node["OutputNodes"].DataDictionary;
            DataList outputNodeKeys = outputNodes.GetKeys();
            for (int i = outputNodeKeys.Count - 1; i >= 0; i--)
            {
                AssignEntryPoint(nodeHash, i, _currentEntryPoint);
                CompileExternOutput(nodeHash, i);
            }
        }
        Debug.Log($"Compiled {node}");
    }
    
    private void InstructionDump()
    {
        string output = "Instruction dump:";
        for (int i = 0; i < _operations.Count; i++)
        {
            if (_operations[i].TokenType == TokenType.Byte)
            {
                output += $"\nInstruction {_operationNames[_operations[i].Byte]}";
            }
            else
            {
                output += $"\nHeap {_operations[i]}: {_heap[_operations[i].Int]}";
            }
        }
        Debug.Log(output);
    }
    
    private int AssignExternHeapAddress(string nodeHash, int index)
    {
        if (!_nodeHeapAddresses.TryGetValue(nodeHash, TokenType.DataDictionary, out DataToken nodeDictionary))
        {
            nodeDictionary = new DataDictionary();
            _nodeHeapAddresses[nodeHash] = nodeDictionary;
        }

        if (nodeDictionary.DataDictionary.TryGetValue(index, out DataToken token))
        {
            return token.Int;
        }
        else
        {
            int address = _heap.Count - 1;
            nodeDictionary.DataDictionary[index] = address;
            return address;
        }
    }
    
    private void AssignNativeHeapAddress(NativeNode node, int index)
    {
        int address = _heap.Count - 1;
        if (!_nodeHeapAddresses.TryGetValue(node, TokenType.DataDictionary, out DataToken nodeDictionary))
        {
            nodeDictionary = new DataDictionary();
            _nodeHeapAddresses[node] = nodeDictionary;
        }

        nodeDictionary.DataDictionary[index] = address;
    }
    private void AssignEntryPoint(string nodeHash, int index, int entryPoint)
    {
        if (!_nodeEntryPoints.TryGetValue(nodeHash, TokenType.DataDictionary, out DataToken nodeDictionary))
        {
            nodeDictionary = new DataDictionary();
            _nodeEntryPoints[nodeHash] = nodeDictionary;
        }

        if (!nodeDictionary.DataDictionary.TryGetValue(index, TokenType.DataList, out DataToken entryPoints))
        {
            entryPoints = new DataList();
            nodeDictionary.DataDictionary[index] = entryPoints;
        }
        
        if (entryPoints.DataList.Contains(entryPoint)) return;
        entryPoints.DataList.Add(entryPoint);
    }


    #endregion
    #region Execution

    private DataList _operationStack = new DataList();
    private int _programCounter;
    
    public void SetHeapVariable(int heapAddress, float value)
    {
        _heap[heapAddress] = value;
    }
    private void StackDump()
    {
        string output = "Stack dump:";
        for (int i = 0; i < _operationStack.Count; i++)
        {
            output += $"\n{_operationStack[i].ToString()}";
        }
        Debug.Log(output);
    }

    
    public void Execute(int entryPoint)
    {
        Debug.Log($"Executing at {entryPoint}");
        _programCounter = 0;
        while (_programCounter < _operations.Count)
        {
            Instruction instruction = (Instruction)_operations[_programCounter].Byte;
            switch (instruction)
            {
                case Instruction.Push:
                {
                    _programCounter++;
                    Push(_heap[_operations[_programCounter].Int]);
                    _programCounter++;
                    break;
                }
                case Instruction.ExternOutput:
                {
                    _programCounter++;
                    break;
                }
                case Instruction.ExternInput:
                {
                    _programCounter++;
                    QueueExternInput(Pop(), Pop(), Pop());
                    break;
                }
                case Instruction.Return:
                {
                    return;
                }
                case Instruction.NOT:
                {
                    _programCounter++;
                    Push((Pop().Float < 0.5f) ? 0.5f : 0f);
                    break;
                }
                case Instruction.AND:
                {
                    _programCounter++;
                    Push((Pop().Float > 0.5f & Pop().Float > 0.5f) ? 1f : 0f);
                    break;
                }
                case Instruction.NAND:
                {
                    _programCounter++;
                    Push((Pop().Float > 0.5f & Pop().Float > 0.5f) ? 0f : 1f);
                    break;
                }
                case Instruction.OR:
                {
                    _programCounter++;
                    Push((Pop().Float > 0.5f | Pop().Float > 0.5f) ? 1f : 0f);
                    break;
                }
                case Instruction.NOR:
                {
                    _programCounter++;
                    Push((Pop().Float > 0.5f | Pop().Float > 0.5f) ? 0f : 1f);
                    break;
                }
                case Instruction.XOR:
                {
                    _programCounter++;
                    Push((Pop().Float > 0.5f ^ Pop().Float > 0.5f) ? 1f : 0f);
                    break;
                }
                case Instruction.XNOR:
                {
                    _programCounter++;
                    Push((Pop().Float > 0.5f ^ Pop().Float > 0.5f) ? 0f : 1f);
                    break;
                }

                case Instruction.Add:
                {
                    Push(Pop().Float + Pop().Float);
                    break;
                }
                case Instruction.Subtract:
                {
                    Push(Pop().Float - Pop().Float);
                    break;
                }
                case Instruction.Multiply:
                {
                    Push(Pop().Float * Pop().Float);
                    break;
                }
                case Instruction.Divide:
                {
                    Push(Pop().Float / Pop().Float);
                    break;
                }
                case Instruction.Modulo:
                {
                    Push(Pop().Float % Pop().Float);
                    break;
                }
                case Instruction.Repeat:
                {
                    Push(Mathf.Repeat(Pop().Float, Pop().Float));
                    break;
                }
                case Instruction.Log:
                {
                    Push(Mathf.Log(Pop().Float, Pop().Float));
                    break;
                }
                case Instruction.Pow:
                {
                    Push(Mathf.Pow(Pop().Float, Pop().Float));
                    break;
                }
                case Instruction.Max:
                {
                    Push(Mathf.Max(Pop().Float, Pop().Float));
                    break;
                }
                case Instruction.Min:
                {
                    Push(Mathf.Min(Pop().Float, Pop().Float));
                    break;
                }
                case Instruction.Abs:
                {
                    Push(Mathf.Abs(Pop().Float));
                    break;
                }
                case Instruction.Cos:
                {
                    Push(Mathf.Cos(Pop().Float));
                    break;
                }
                case Instruction.Acos:
                {
                    Push(Mathf.Acos(Pop().Float));
                    break;
                }
                case Instruction.Sin:
                {
                    Push(Mathf.Sin(Pop().Float));
                    break;
                }
                case Instruction.Asin:
                {
                    Push(Mathf.Asin(Pop().Float));
                    break;
                }
                case Instruction.Tan:
                {
                    Push(Mathf.Tan(Pop().Float));
                    break;
                }
                case Instruction.Atan:
                {
                    Push(Mathf.Atan(Pop().Float));
                    break;
                }
                case Instruction.Round:
                {
                    Push(Mathf.Round(Pop().Float));
                    break;
                }
                case Instruction.Floor:
                {
                    Push(Mathf.Floor(Pop().Float));
                    break;
                }
                case Instruction.Ceil:
                {
                    Push(Mathf.Ceil(Pop().Float));
                    break;
                }
                case Instruction.Sign:
                {
                    Push(Mathf.Sign(Pop().Float));
                    break;
                }
                case Instruction.Sqrt:
                {
                    Push(Mathf.Sqrt(Pop().Float));
                    break;
                }
                case Instruction.Clamp:
                {
                    Push(Mathf.Clamp(Pop().Float, Pop().Float, Pop().Float));
                    break;
                }
                case Instruction.InverseLerp:
                {
                    Push(Mathf.InverseLerp(Pop().Float, Pop().Float, Pop().Float));
                    break;
                }
                case Instruction.Lerp:
                {
                    Push(Mathf.Lerp(Pop().Float, Pop().Float, Pop().Float));
                    break;
                }
                case Instruction.SmoothStep:
                {
                    Push(Mathf.SmoothStep(Pop().Float, Pop().Float, Pop().Float));
                    break;
                }
                case Instruction.MoveTowards:
                {
                    Push(Mathf.MoveTowards(Pop().Float, Pop().Float, Pop().Float));
                    break;
                }
                case Instruction.Equal:
                {
                    Push(Mathf.Approximately(Pop().Float, Pop().Float) ? 1f : 0f);
                    break;
                }
                case Instruction.NotEqual:
                {
                    Push(Mathf.Approximately(Pop().Float, Pop().Float) ? 0f : 1f);
                    break;
                }
                case Instruction.Greater:
                {
                    Push((Pop().Float > Pop().Float) ? 1f : 0f);
                    break;
                }
                case Instruction.GreaterEqual:
                {
                    Push((Pop().Float >= Pop().Float) ? 1f : 0f);
                    break;
                }
                case Instruction.Less:
                {
                    Push((Pop().Float < Pop().Float) ? 1f : 0f);
                    break;
                }
                case Instruction.LessEqual:
                {
                    Push((Pop().Float <= Pop().Float) ? 1f : 0f);
                    break;
                }
                default:
                {
                    Debug.LogError($"Unexpected instruction {instruction}");
                    break;
                }
            }
        }
    }


    #endregion

    #region Building

    
    public GameObject wireTemplate;
    public Transform templates;
    private DataDictionary _templateDictionary = new DataDictionary();
    private DataDictionary _nodeProperties = new DataDictionary();
    private DataList _outputNodes = new DataList();
    
    private DataDictionary _nodeHashToObject = new DataDictionary();
    private DataDictionary _nodeObjectToHash = new DataDictionary();
    
    private DataDictionary _wireProperties = new DataDictionary();
    private DataDictionary _wireHashToObject = new DataDictionary();
    private DataDictionary _wireObjectToHash = new DataDictionary();

    #endregion
    
    #region interpreter

    private bool _isDirty;
    
    [RecursiveMethod]
    private void MarkNodeDirty(string nodeHash)
    {
        if (!_nodeProperties.TryGetValue(nodeHash, TokenType.DataDictionary, out DataToken token)) return;
        DataDictionary node = token.DataDictionary;
        if (node["Dirty"].Boolean) return;

        node["Dirty"] = true;
        DataDictionary outputNodes = node["OutputNodes"].DataDictionary;
        DataList outputNodeKeys = outputNodes.GetKeys();
        for (int i = 0; i < outputNodeKeys.Count; i++)
        {
            MarkNodeDirty(outputNodes[outputNodeKeys[i]].String);
        }
    }

    private float NodeGetOutputValue(string nodeHash, int slot)
    {
        DataDictionary node = _nodeProperties[nodeHash].DataDictionary;
        DataDictionary values = _nodeProperties["OutputValues"].DataDictionary;
        if (!node["Dirty"].Boolean)
        {
            if (values.TryGetValue(slot.ToString(), TokenType.Float, out DataToken oldValue))
            {
                return oldValue.Float;
            }
            else
            {
                return 0f;
            }
        }
        EvaluateNode(node);
        
        if (values.TryGetValue(slot.ToString(), TokenType.Float, out DataToken newValue))
        {
            return newValue.Float;
        }
        else
        {
            return 0f;
        }
    }
    
    private void EvaluateNode(DataDictionary node)
    {
        DataDictionary inputNodes = node["InputNodes"].DataDictionary;
        DataList inputNodeKeys = inputNodes.GetKeys();
        for (int i = 0; i < inputNodeKeys.Count; i++)
        {
            
        }
    }

    #endregion
    
    private void Update()
    {
        if (_needsCompile)
        {
            Compile();
            _needsCompile = false;
        }
        if (_pendingOutputNodes.Count == 0) return;
        ApplyExternOutputs();
        ApplyExternInputs();
    }


    private DataList _pendingOutputNodes = new DataList();
    public void QueueExternOutput(DataToken node, int index)
    {
        if (_pendingOutputNodes.Contains(node)) return;
        _pendingOutputNodes.Add(node);
        MarkNodeDirty(_nodeProperties[node].String);
    }

    private void ApplyExternOutputs()
    {
        for (int i = 0; i < _pendingOutputNodes.Count; i++)
        {
            SetHeapAddresses((ExternNode)_pendingOutputNodes[i].Reference);
            FindEntryPoints((ExternNode)_pendingOutputNodes[i].Reference);
        }

        for (int i = 0; i < _pendingEntryPoints.Count; i++)
        {
            Execute(_pendingEntryPoints[i].Int);
        }
        _pendingOutputNodes.Clear();
        _pendingEntryPoints.Clear();
    }

    private void SetHeapAddresses(ExternNode node)
    {
        if (!_nodeHeapAddresses.TryGetValue(node, TokenType.DataDictionary, out DataToken heapContainer))
        {
            Debug.LogError($"Node {node} did not have any assigned heap addresses");
            return;
        }

        DataList indexes = heapContainer.DataDictionary.GetKeys();
        for (int i = 0; i < indexes.Count; i++)
        {
            int heapAddress = heapContainer.DataDictionary[indexes[i]].Int;
            float value = node.GetOutput(indexes[i].Int);
            SetHeapVariable(heapAddress, value);
            Debug.Log($"Set heap address {heapAddress} to {value}");
        }
    }

    private void FindEntryPoints(ExternNode node)
    {
        Debug.Log($"Finding entry points for {node}");
        if (!_nodeEntryPoints.TryGetValue(node, TokenType.DataDictionary, out DataToken entryPointContainer))
        {
            Debug.LogError($"Node {node} did not have any assigned entry points");
            return;
        }

        DataList indexes = entryPointContainer.DataDictionary.GetKeys();
        for (int i = 0; i < indexes.Count; i++)
        {
            DataList entryPoints = entryPointContainer.DataDictionary[indexes[i]].DataList;
            for (int j = 0; j < entryPoints.Count; j++)
            {
                if (_pendingEntryPoints.Contains(entryPoints[j])) continue;
                _pendingEntryPoints.Add(entryPoints[j]);
                Debug.Log($"Found entry point {_pendingEntryPoints}");
            }
        }
    }

    private DataList _pendingEntryPoints = new DataList();

    private DataList pendingExternInputNode = new DataList();
    private DataList pendingExternInputValue = new DataList();
    private DataList pendingExternInputIndex = new DataList();
    private void QueueExternInput(DataToken node, DataToken index, DataToken value)
    {
        Debug.Log($"Queued extern node {node} index {index} value {value}");
        pendingExternInputNode.Add(node);
        pendingExternInputIndex.Add(index);
        pendingExternInputValue.Add(value);
    }

    private void ApplyExternInputs()
    {
        for (int i = 0; i < pendingExternInputNode.Count; i++)
        {
            ExternNode node = (ExternNode)pendingExternInputNode[i].Reference;
            node.SetInput(pendingExternInputIndex[i].Int, pendingExternInputValue[i].Float);
        }
        pendingExternInputNode.Clear();
        pendingExternInputIndex.Clear();
        pendingExternInputValue.Clear();
    }

    private void Push(DataToken value)
    {
        _operationStack.Insert(0, value);
    }
    private DataToken Pop()
    {
        DataToken output = _operationStack[0];
        _operationStack.RemoveAt(0);
        return output;
    }




    public TextAsset defaultWorldState;
    void Start()
    {
        for (int i = 0; i < templates.childCount; i++)
        {
            _templateDictionary[templates.GetChild(i).name] = templates.GetChild(i);
        }

        if (defaultWorldState != null)
        {
            LoadWorldState(defaultWorldState.text);
        }
    }
    
    private void LoadWorldState(string state)
    {
        if (!VRCJson.TryDeserializeFromJson(state, out DataToken result))
        {
            Debug.LogError($"Unable to deserialize {result}");
            return;
        }

        DataDictionary nodes = result.DataDictionary["Nodes"].DataDictionary;
        DataDictionary wires = result.DataDictionary["Wires"].DataDictionary;
        DataList nodeKeys = nodes.GetKeys();
        for (int i = 0; i < nodeKeys.Count; i++)
        {
            if (_nodeProperties.ContainsKey(nodeKeys[i])) continue;
            _nodeProperties[nodeKeys[i]] = nodes[nodeKeys[i]];
            InstantiateNode(nodeKeys[i].String);
        }
        DataList wireKeys = wires.GetKeys();
        for (int i = 0; i < wireKeys.Count; i++)
        {
            if (_wireProperties.ContainsKey(wireKeys[i])) continue;
            _wireProperties[wireKeys[i]] = wires[wireKeys[i]];
            SetupWire(wireKeys[i].String);
        }
    }
    public void NewNode(string template, Vector3 position, Vector3 rotation)
    {
        if (!_templateDictionary.ContainsKey(template))
        {
            Debug.LogError($"template {template} does not exist");
            return;
        }
        
        DataDictionary newNode = new DataDictionary();
        newNode["Template"] = template;
        newNode["Position"] = VectorToList(position);
        newNode["Rotation"] = VectorToList(rotation);
        newNode["InputNodes"] = new DataDictionary();
        newNode["OutputNodes"] = new DataDictionary();
        newNode["OutputValues"] = new DataDictionary();
        newNode["InputWires"] = new DataDictionary();
        newNode["OutputWires"] = new DataDictionary();
        newNode["Dirty"] = true;
        //newNode["NumInputs"] = 0;
        //newNode["NumOutputs"] = 0;
        //newNode["Operation"] = (int)Instruction.XOR;
        
        string hash = GetNewHash();
        _nodeProperties[hash] = newNode;

        InstantiateNode(hash);
    }

    public bool TryGetNodeProperty(CircuitsNode node, DataToken key, out DataToken value)
    {
        if (!_nodeObjectToHash.TryGetValue(node, out DataToken nodeHash)) { value = new DataToken(); return false;}
        if (!_nodeProperties.TryGetValue(node, out DataToken nodeProperties)) { value = new DataToken(); return false;}
        return nodeProperties.DataDictionary.TryGetValue(key, out value);
    }
    public void SetNodeProperty(CircuitsNode node, DataToken key, DataToken value)
    {
        if (!_nodeObjectToHash.TryGetValue(node, out DataToken nodeHash)) return;
        if (!_nodeProperties.TryGetValue(node, out DataToken nodeProperties)) return;
        nodeProperties.DataDictionary[key] = value;
    }
    
    public void NewWire(CircuitsNode outputNode, int outputSlot, DataList positions, CircuitsNode inputNode, int inputSlot)
    {
        string inputHash = GetObjectHash(inputNode.gameObject);
        string outputHash = GetObjectHash(outputNode.gameObject);
        DataDictionary newWire = new DataDictionary();
        newWire["OutputSlot"] = outputSlot;
        newWire["InputSlot"] = inputSlot;
        newWire["InputNode"] = inputHash;
        newWire["OutputNode"] = outputHash;
        newWire["Positions"] = positions;

        if (positions.Count == 0)
        {
            positions.Add(VectorToList(outputNode.GetNearestWirePosition(outputSlot, false, inputNode.transform.position)));
            positions.Add(VectorToList(inputNode.GetNearestWirePosition(inputSlot, true, outputNode.transform.position)));
        }
        else
        {
            Vector3 lastPosition = inputNode.GetNearestWirePosition(inputSlot, true, (Vector3)positions[positions.Count - 1].Reference);
            for (int i = 0; i < positions.Count; i++)
            {
                positions[i] = VectorToList((Vector3)positions[i].Reference);
            }
            positions.Add(VectorToList(lastPosition));

        }
        string hash = GetNewHash();
        _wireProperties[hash] = newWire;
        _needsCompile = true;
        SetupWire(hash);
    }

    private void SetupWire(string hash)
    {
        DataDictionary properties = _wireProperties[hash].DataDictionary;
        NodeSetInput(properties["InputNode"].String, Convert.ToInt32(properties["InputSlot"].Number), properties["OutputNode"].String, hash);
        NodeSetOutput(properties["OutputNode"].String, Convert.ToInt32(properties["OutputSlot"].Number), properties["InputNode"].String, hash);
        
        InstantiateWire(hash);
    }
    
    private void NodeSetInput(string nodeHash, int slot, string otherNodeHash, string wireHash)
    {
        Debug.Log($"Setting input {slot} of node {nodeHash} to {wireHash}");
        DataDictionary nodeProperties = _nodeProperties[nodeHash].DataDictionary;
        DataDictionary inputWires = nodeProperties["InputWires"].DataDictionary;
        DataDictionary inputNodes = nodeProperties["InputNodes"].DataDictionary;
        if (inputWires.ContainsKey(slot.ToString()))
        {
            ClearInput(nodeHash, slot);
        }
        inputWires[slot.ToString()] = wireHash;
        inputNodes[slot.ToString()] = otherNodeHash;
        
        if (nodeProperties["Template"] != "NativeNode" && !_outputNodes.Contains(nodeHash))
        {
            _outputNodes.Add(nodeHash);
        }
    }

    private void ClearInput(string nodeHash, int slot)
    {
        Debug.Log($"Clearing input {slot} of node {nodeHash}");
        DataDictionary nodeProperties = _nodeProperties[nodeHash].DataDictionary;
        DataDictionary inputWires = nodeProperties["InputWires"].DataDictionary;
        DestroyWire(inputWires[slot.ToString()].String);
    }
    
    private void DestroyWire(string wireHash)
    {
        if (!_wireHashToObject.TryGetValue(wireHash, TokenType.DataList, out DataToken token))
        {
            Debug.Log($"Unable to destroy wire {wireHash}: not recognized");
            return;
        }

        DataList wires = token.DataList;
        for (int i = 0; i < wires.Count; i++)
        {
            _wireObjectToHash.Remove((GameObject)wires[i].Reference);
            Destroy((GameObject)wires[i].Reference);
        }
        _wireHashToObject.Remove(wireHash);
        _wireProperties.Remove(wireHash);
    }

    
    private void NodeSetOutput(string nodeHash, int slot, string otherNodeHash, string wireHash)
    {
        Debug.Log($"Setting output {slot} of node {nodeHash} to {wireHash}");
        DataDictionary nodeProperties = _nodeProperties[nodeHash].DataDictionary;
        DataDictionary outputWires = nodeProperties["OutputWires"].DataDictionary;
        DataDictionary outputNodes = nodeProperties["OutputNodes"].DataDictionary;
        DataDictionary outputValues = nodeProperties["OutputValues"].DataDictionary;
        if (!outputWires.ContainsKey(slot)) outputWires[slot.ToString()] = new DataList();
        if (!outputNodes.ContainsKey(slot)) outputNodes[slot.ToString()] = new DataList();
        outputWires[slot.ToString()].DataList.Add(wireHash);
        outputNodes[slot.ToString()].DataList.Add(otherNodeHash);
        outputValues[slot.ToString()] = 0f;
    }

    private void InstantiateWire(string wireHash)
    {
        if (!_wireProperties.ContainsKey(wireHash)) return;
        DataDictionary properties = _wireProperties[wireHash].DataDictionary;

        DataList positions = properties["Positions"].DataList;
        DataList wires = new DataList();
        Vector3 lastPosition = ListToVector(positions[0].DataList);
        for (int i = 1; i < positions.Count; i++)
        {
            Vector3 nextPosition = ListToVector(positions[i].DataList);
            GameObject newWire = Instantiate(wireTemplate);
            wires.Add(newWire);
            newWire.SetActive(true);
            Transform wireTransform = newWire.transform;
            wireTransform.position = lastPosition;
            wireTransform.LookAt(nextPosition);
            wireTransform.localScale = new Vector3(1, 1, Vector3.Distance(lastPosition, nextPosition));
            lastPosition = nextPosition;
            _wireObjectToHash[newWire] = wireHash;
        }

        _wireHashToObject[wireHash] = wires;
    }

    private void InstantiateNode(string hash)
    {
        if (!_nodeProperties.ContainsKey(hash)) return;
        DataDictionary container = _nodeProperties[hash].DataDictionary;
        string template = container["Template"].String;
        GameObject instantiated = Instantiate(((Transform)_templateDictionary[template].Reference).gameObject);
        instantiated.transform.SetPositionAndRotation(ListToVector(container["Position"].DataList), Quaternion.Euler(ListToVector(container["Rotation"].DataList)));
        _nodeHashToObject[hash] = instantiated;
        _nodeObjectToHash[instantiated] = hash;
    }

    private void DestroyNode(string hash)
    {
        DataDictionary node = _nodeProperties[hash].DataDictionary;
        DataDictionary inputWires = node["InputWires"].DataDictionary;
        DataList inputWireKeys = inputWires.GetKeys();
        for (int i = 0; i < inputWireKeys.Count; i++)
        {
            DestroyWire(inputWires[inputWireKeys].String);
        }
        DataDictionary outputWires = node["OutputWires"].DataDictionary;
        DataList outputWireKeys = outputWires.GetKeys();
        for (int i = 0; i < outputWireKeys.Count; i++)
        {
            DataList outputWireSlot = outputWires[outputWireKeys].DataList;
            for (int j = 0; j < outputWireSlot.Count; j++)
            {
                DestroyWire(outputWireSlot[j].String);
            }
        }
        _outputNodes.Remove(hash);
    }
    
    private DataList VectorToList(Vector3 input)
    {
        DataList output = new DataList();
        output.Add(input.x);
        output.Add(input.y);
        output.Add(input.z);
        return output;
    }
    private Vector3 ListToVector(DataList input)
    {
        Vector3 output = new Vector3();
        output.x = (float)input[0].Number;
        output.y = (float)input[1].Number;
        output.z = (float)input[2].Number;
        return output;
    }

    public void RemoveNode(GameObject nodeObject)
    {
        if (!Utilities.IsValid(nodeObject))
        {
            Debug.Log("Unable to remove node: provided gameobject was not valid");
            return;
        }
        if (!_nodeObjectToHash.ContainsKey(nodeObject))
        {
            Debug.Log($"Unable to remove node: provided gameObject '{nodeObject.name}' is not recognized");
            return;
        }
        RemoveNode(_nodeObjectToHash[nodeObject].String);
    }
    
    public void RemoveNode(string hash)
    {
        if (!_nodeHashToObject.ContainsKey(hash))
        {
            Debug.Log($"Unable to remove node: provided hash '{hash}' is not recognized");
            return;
        }

        GameObject node = (GameObject)_nodeHashToObject[hash].Reference;
        _nodeProperties.Remove(hash);
        _nodeHashToObject.Remove(hash);
        _nodeObjectToHash.Remove(node);
        Destroy(node);
    }
    
    public DataList GetAllTemplateNames()
    {
        return _templateDictionary.GetKeys();
    }

    public string GetObjectHash(GameObject nodeObject)
    {
        return _nodeObjectToHash[nodeObject].String;
    }
    
    private string GetNewHash()
    {
        System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
        int time = (int)(System.DateTime.UtcNow - epochStart).TotalMinutes;
        
        string hash = CalculateHash(time.ToString() + Time.realtimeSinceStartup).ToString();
        while (_nodeProperties.ContainsKey(hash) || _wireProperties.ContainsKey(hash))
        {
            hash = CalculateHash(time.ToString() + Time.realtimeSinceStartup).ToString();
        }
        return hash;
    }
    private ulong CalculateHash(string read)
    {
        ulong hashedValue = 3074457345618258791ul;
        for(int i=0; i<read.Length; i++)
        {
            hashedValue += read[i];
            hashedValue *= 3074457345618258799ul;
        }
        return hashedValue;
    }
}