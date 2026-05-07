
using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

public class Wrench : UdonSharpBehaviour
{
    public CircuitsManager circuitsManager;
    public Transform panelParent;
    public Transform inputButtons;
    public Transform outputButtons;
    public Transform carriedWire;

    private CircuitsNode _selectedNode;
    private CircuitsNode _carriedNode;
    
    private int _carriedNodeIndex;
    private bool _carriedIsInput;
    private LayerMask _layers = 1;
    private void Update()
    {
        if (Utilities.IsValid(_carriedNode))
        {
            carriedWire.gameObject.SetActive(true);
            ProcessCarriedWire();
        }
        else
        {
            carriedWire.gameObject.SetActive(false);
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SpawnNode(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SpawnNode(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SpawnNode(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            SpawnNode(3);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            SpawnNode(4);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            SpawnNode(5);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            SpawnNode(6);
        }
    }

    private void SpawnNode(int index)
    {
        if (!circuitsManager.GetAllTemplateNames().TryGetValue(index, out DataToken template))
        {
            Debug.LogError($"Unable to spawn template: {template}");
        }

        VRCPlayerApi.TrackingData trackingData = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);

        if (!Physics.Raycast(trackingData.position, trackingData.rotation * Vector3.forward, out RaycastHit hit, 5f, _layers)) return;
        
        circuitsManager.NewNode(template.String, RoundVector(hit.point), Vector3.zero);
    }

    private Vector3 RoundVector(Vector3 input)
    {
        input.x = Mathf.Round(input.x * 10f) / 10f;
        input.y = Mathf.Round(input.y * 10f) / 10f;
        input.z = Mathf.Round(input.z * 10f) / 10f;
        return input;
    }

    private Vector3 _previousCarriedPosition;
    private DataList _carriedWirePositions = new DataList();
    private DataList _carriedWires = new DataList();
    private void ProcessCarriedWire()
    {
        RaycastHit hit;
        Vector3 position;
        if (_carriedWirePositions.Count == 0)
        {
            position = _carriedNode.GetNearestWirePosition(_carriedNodeIndex, _carriedIsInput, carriedWire.position);
            carriedWire.LookAt(position);
            carriedWire.localScale = new Vector3(1, 1, Vector3.Distance(carriedWire.position, position));
        }
        else
        {
            position = (Vector3)_carriedWirePositions[_carriedWirePositions.Count - 1].Reference;
            carriedWire.LookAt(position);
            carriedWire.localScale = new Vector3(1, 1, Vector3.Distance(carriedWire.position, position));
        }

        if (_carriedWirePositions.Count > 1 && IsPreviousClear())
        {
            RemovePosition();
            ProcessCarriedWire();
            return;
        }
        
        //Check if current path is blocked

        IsBlocked(carriedWire.position, position,Vector3.Max(carriedWire.position - _previousCarriedPosition, Vector3.Normalize(carriedWire.position - _previousCarriedPosition) * 0.1f));
        
        _previousCarriedPosition = carriedWire.position;

        for (int i = 0; i + 1 < _carriedWirePositions.Count; i++)
        {
            Debug.DrawLine((Vector3)_carriedWirePositions[i].Reference, (Vector3)_carriedWirePositions[i + 1].Reference);
        }
    }

    private bool IsBlocked(Vector3 start, Vector3 end, Vector3 direction)
    {
        if (!Physics.Linecast(start, end, out RaycastHit hit, _layers, QueryTriggerInteraction.Ignore)) return false;
        if (!Utilities.IsValid(hit.collider)) return false;
        if (hit.collider.GetComponentInParent<CircuitsNode>() == _carriedNode) return false;

        Vector3 originalPoint = hit.point;
        Vector3 originalNormal = hit.normal;
        
        Vector3 side = -direction;
        Debug.DrawRay(originalPoint, side, Color.white, 1);
        
        Vector3 reverse = -originalNormal * direction.magnitude;
        Debug.DrawRay(originalPoint + side, reverse, Color.white, 1);

        Vector3 final = originalPoint + side + reverse;
        if (!Physics.Raycast(final, -side, out hit, direction.magnitude * 2, _layers, QueryTriggerInteraction.Ignore))
        {
            Debug.Log("Side raycast failed");
            AddNewPosition(originalPoint);
            return true;
        }
        Debug.DrawLine(final, hit.point, Color.red, 1);

        Vector3 sidePoint = hit.point;
        Vector3 sideNormal = hit.normal;

        Vector3 corner = Vector3.ProjectOnPlane(sidePoint - originalPoint, originalNormal) + originalPoint;
        Debug.DrawRay(corner, Vector3.up, Color.white, 1);

        AddNewPosition(corner);
        return true;
    }
    private void RemovePosition()
    {
        int index = _carriedWires.Count - 1;
        if (!_carriedWires[index].IsNull)
        {
            Destroy((GameObject)_carriedWires[index].Reference);
        }
        _carriedWires.RemoveAt(index);
        _carriedWirePositions.RemoveAt(index);
        if (index == 1) RemovePosition();
    }
    private void AddNewPosition(Vector3 position)
    {
        if (_carriedWirePositions.Count == 0)
        {
            _carriedWirePositions.Add(new DataToken(_carriedNode.GetNearestWirePosition(_carriedNodeIndex, _carriedIsInput, position)));
            _carriedWires.Add(new DataToken());
        }

        if (Vector3.Distance((Vector3)_carriedWirePositions[_carriedWirePositions.Count - 1].Reference, position) < 0.1f)
        {
            return;
        }
        _carriedWires.Add(NewWire(position, (Vector3)_carriedWirePositions[_carriedWirePositions.Count - 1].Reference));
        _carriedWirePositions.Add(new DataToken(position));
    }
    
    private GameObject NewWire(Vector3 a, Vector3 b)
    {
        GameObject newWire = Instantiate(carriedWire.gameObject);
        newWire.SetActive(true);
        Transform wireTransform = newWire.transform;
        wireTransform.position = a;
        wireTransform.LookAt(b);
        wireTransform.localScale = new Vector3(1, 1, Vector3.Distance(a, b));
        return newWire;
    }

    private bool IsPreviousClear()
    {
        Vector3 a = carriedWire.position;
        Vector3 b = (Vector3)_carriedWirePositions[_carriedWirePositions.Count - 1].Reference;
        Vector3 c = (Vector3)_carriedWirePositions[_carriedWirePositions.Count - 2].Reference;

        Vector3 acNormalized = Vector3.Normalize(c - a);
        float dot = Vector3.Dot(acNormalized, b - a);
        Vector3 d = a + acNormalized * dot;
        
        bool hit = Physics.Linecast(b + Vector3.Normalize(a-b) * 0.01f + Vector3.Normalize(b-c) * 0.01f, d, _layers);
        Debug.DrawLine(b + Vector3.Normalize(a-b) * 0.01f + Vector3.Normalize(b-c) * 0.01f, d, hit ? Color.red : Color.green, 1);
        return !hit;
    }
    
    public override void OnPickupUseDown()
    {
        RaycastHit hit;
        VRCPlayerApi player = Networking.LocalPlayer;
        if (Physics.Raycast(player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position, player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation * Vector3.forward, out hit, 5) && Utilities.IsValid(hit.collider))
        {
            CircuitsNode node = hit.collider.GetComponentInParent<CircuitsNode>();
            if (!Utilities.IsValid(node))
            {
                Debug.Log($"Couldn't find a node on {hit.transform.name}");
                return;
            }
            OpenNode(node);
        }
    }

    public override void OnDrop()
    {
        if (Utilities.IsValid(_carriedNode))
        {
            DropNode();
        }

        if (Utilities.IsValid(_selectedNode))
        {
            CloseNode();
        }
    }

    public void OpenNode(CircuitsNode node)
    {
        panelParent.gameObject.SetActive(true);
        Transform panelPosition = node.GetNearestPanelPosition();
        panelParent.SetPositionAndRotation(panelPosition.position, panelPosition.rotation);
        _selectedNode = node;
        for (int i = 0; i < inputButtons.childCount; i++)
        {
            inputButtons.GetChild(i).gameObject.SetActive(false);
        }
        for (int i = 0; i < outputButtons.childCount; i++)
        {
            outputButtons.GetChild(i).gameObject.SetActive(false);
        }

        if (node.inputNames != null)
        {
            for (int i = 0; i < node.inputNames.Length; i++)
            {
                inputButtons.GetChild(i).gameObject.SetActive(true);
                inputButtons.GetChild(i).GetComponentInChildren<Text>().text = node.inputNames[i];
            }
        }
        else
        {
            Debug.Log($"Node has no inputs");
        }

        if (node.outputNames != null)
        {
            for (int i = 0; i < node.outputNames.Length; i++)
            {
                outputButtons.GetChild(i).gameObject.SetActive(true);
                outputButtons.GetChild(i).GetComponentInChildren<Text>().text = node.outputNames[i];
            }
        }
        else
        {
            Debug.Log($"Node has no outputs");
        }
    }

    private void CarryNode(CircuitsNode node, int slot, bool isInput)
    {
        _carriedNode = node;
        _carriedNodeIndex = slot;
        _carriedIsInput = isInput;
        Debug.Log($"Now carrying {_carriedNode.name}");
        _carriedWirePositions.Clear();
        carriedWire.gameObject.SetActive(true);
    }

    private void DropNode()
    {
        _carriedNode = null;
        carriedWire.gameObject.SetActive(false);
        for (int i = 0; i < _carriedWires.Count; i++)
        {
            if (_carriedWires[i].IsNull) continue;
            Destroy((GameObject)_carriedWires[i].Reference);
        }
        _carriedWires.Clear();
        _carriedWirePositions.Clear();
    }
    public void CloseNode()
    {
        panelParent.gameObject.SetActive(false);
        _selectedNode = null;
    }

    public Slider buttonFloatArgument;
    public Toggle buttonBoolArgument;
    public Text buttonStringArgument;
    
    public void ButtonPressed()
    {
        switch (buttonStringArgument.text)
        {
            case "ClickInput":
            {
                if (Utilities.IsValid(_carriedNode))
                {
                    if (_carriedIsInput)
                    {
                        Debug.Log("Cannot connect input to input");
                    }
                    else
                    {
                        circuitsManager.NewWire(_carriedNode, _carriedNodeIndex, _carriedWirePositions.ShallowClone(), _selectedNode, Mathf.RoundToInt(buttonFloatArgument.value));
                        DropNode();
                    }
                }
                else
                {
                    CarryNode(_selectedNode, Mathf.RoundToInt(buttonFloatArgument.value), true);
                }
                break;
            }
            case "ClickOutput":
            {
                if (Utilities.IsValid(_carriedNode))
                {
                    if (_carriedIsInput)
                    {
                        circuitsManager.NewWire(_selectedNode, Mathf.RoundToInt(buttonFloatArgument.value), _carriedWirePositions.ShallowClone(),  _carriedNode, _carriedNodeIndex);
                        DropNode();
                    }
                    else
                    {
                        Debug.Log("Cannot connect output to output");
                    }
                }
                else
                {
                    CarryNode(_selectedNode, Mathf.RoundToInt(buttonFloatArgument.value), false);
                }
                break;
            }
        }
    }
}
