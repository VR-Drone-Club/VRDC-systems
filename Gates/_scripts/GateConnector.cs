
using System;
using System.Diagnostics;
using UdonSharp;
using UdonToolkit;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;
using Debug = UnityEngine.Debug;

public class GateConnector : UdonSharpBehaviour
{
    public DroneGate[] gates;
    public LineRenderer guideLine;
    public RaceHud raceHud;
    
    private int _lastGate;
    private int _nextGate;
    private Stopwatch _stopwatch;
    private LapRecord _currentLapRecord;
    private PlayerStats _localPlayerStats;
    
    //private GateLap gateOrder;
    //private DataStack progressStack;
    
    private void Start()
    {
        _localPlayerStats = PlayerStats.Find(Networking.LocalPlayer);
        if (!Utilities.IsValid(gates)) return;
        foreach (var gate in gates)
        {
            gate.RegisterConnector(this);
        }
        CalculateCurve();

        _lastGate = _nextGate;
        _nextGate = 0;
        ApplyState();
        //gateOrder = GateLap.New(0);
        //progressStack = DataStack.New();
    }

    internal virtual void BeginCourse()
    {
        _lastGate = 0;
        _nextGate = 1;
        _stopwatch = Stopwatch.StartNew();
        _currentLapRecord = LapRecord.Create();
        ApplyState();
    }

    internal virtual void AbortCourse()
    {
        if (Utilities.IsValid(_stopwatch)) _stopwatch.Stop();
        _lastGate = 0;
        _nextGate = 0;
        ApplyState();
        guideLine.positionCount = 0;
    }
    internal virtual void EndCourse()
    {
        _localPlayerStats.SubmitLap(name, _currentLapRecord);
    }

    public void GateTriggered(DroneGate gate)
    {
        if (!Utilities.IsValid(gate)) return;
        int index = Array.IndexOf(gates, gate);
        if (index == -1) return;
        Debug.Log($"Went through gate {index} while expected next gate was {_nextGate}");
        if (gates[index] != gates[_nextGate])
        {
            if (gates[0] == gate)
            {
                AbortCourse();
                BeginCourse();
                return;
            }
            else
            {
                return;
            }
        }
        NextGate();
    }
    
    [Button("NextGate")]
    public void NextGate()
    {
        if (_nextGate == 0)
        {
            BeginCourse();
            ApplyState();
            return;
        }
        if (_nextGate + 1 >= gates.Length)
        {
            if (gates[0] == gates[_nextGate]) //Repeat if the start gate is same as end gate
            {
                Split();
                EndCourse();
                BeginCourse();
            }
            else // Or just end if this doesn't loop
            {
                Split();
                EndCourse();
                _lastGate = _nextGate;
                _nextGate = 0;
            }
        }
        else
        {
            Split();
            _lastGate = _nextGate;
            _nextGate++;
        }
    
        _pointOnCurve = _lastGate * curveResolution;
        ApplyState();
    }

    private void Split()
    {
        if (_stopwatch == null) return;
        _currentLapRecord.AddSplit(_stopwatch.Elapsed.TotalSeconds);
        if (Utilities.IsValid(raceHud)) raceHud.DisplaySplit(name, _currentLapRecord);

        string output = string.Empty;
        for (int i = 0; i < _currentLapRecord.Count; i++)
        {
            output += _currentLapRecord[i] + " ";
        }
        Debug.Log($"Split {output}");
    }
    private void ApplyState()
    {
        for (int i = 0; i < gates.Length; i++)
        {
            if (_nextGate != i)
            {
                gates[i].State = GateState.Idle;
            }
        }

        for (int i = 0; i < gates.Length; i++)
        {
            if (_nextGate == i)
            {
                gates[i].State = GateState.EncourageEntry;
            }
        }
    }
    private Vector3[] _curve;
    public int curveResolution;
    public float curveFadeDistance;
    private Vector3 _curveStart;
    private Vector3 _curveStartDirection;
    private Vector3 _curveEnd;
    private Vector3 _curveEndDirection;

    private void Update()
    {
        if (!Utilities.IsValid(guideLine)) return;
        DrawCurveProgressive();
        //if (Vector3.Distance(guideLine.GetPosition(0), Networking.LocalPlayer.GetDrone().GetPosition()) < curveFadeDistance)
        if (Utilities.IsValid(_stopwatch) && !Networking.LocalPlayer.GetDrone().IsDeployed())
        {
            AbortCourse();
        }
        if (_pointOnCurve < _nextGate * curveResolution && Vector3.Dot(guideLine.GetPosition(1) - guideLine.GetPosition(0), Networking.LocalPlayer.GetDrone().GetPosition() - guideLine.GetPosition(0)) > 0)
        {
            _pointOnCurve = (_pointOnCurve + 1) % _curve.Length;
            DrawCurveProgressive();
        }
    }

    public void DrawCurve()
    {
        return;
        Transform startTransform = gates[_nextGate].transform;
        Transform endTransform = gates[_lastGate].transform;
        _curveStart = startTransform.position;
        if (Utilities.IsValid(gates[_nextGate].reverseControlPoint))
        {
            _curveStartDirection = gates[_nextGate].reverseControlPoint.position;
        }
        else
        {
            _curveStartDirection = (startTransform.forward * -1 * Vector3.Distance(startTransform.position, endTransform.position) / 2) + _curveStart;
        }
        _curveEnd = endTransform.position;
        if (Utilities.IsValid(gates[_lastGate].forwardControlPoint))
        {
            _curveEndDirection = gates[_lastGate].forwardControlPoint.position;
        }
        else
        {
            _curveEndDirection = (endTransform.forward * Vector3.Distance(startTransform.position, endTransform.position) / 2) + _curveEnd;
        }
        
        if (!Utilities.IsValid(_curve) || _curve.Length != curveResolution || guideLine.positionCount != curveResolution)
        {
            _curve = new Vector3[curveResolution];
            guideLine.positionCount = curveResolution;
        }
        for (int i = 0; i < _curve.Length; i++)
        {
            _curve[i] = GetPointAtTime((float)i / (_curve.Length-1));
        }
        guideLine.SetPositions(_curve);
    }

    public void CalculateCurve()
    {
        if (!Utilities.IsValid(_curve) || guideLine.positionCount != curveResolution * gates.Length)
        {
            _curve = new Vector3[curveResolution * gates.Length];
            guideLine.positionCount = curveResolution * gates.Length;
        }

        int curveIndex = 0;
        for (int gateIndex = 0; gateIndex < gates.Length - 1; gateIndex++)
        {
            Transform startTransform = gates[gateIndex].transform;
            Transform endTransform = gates[gateIndex + 1].transform;
            _curveStart = startTransform.position;
            if (Utilities.IsValid(gates[gateIndex].forwardControlPoint))
            {
                _curveStartDirection = gates[gateIndex].forwardControlPoint.position;
            }
            else
            {
                _curveStartDirection = (startTransform.forward * Vector3.Distance(startTransform.position, endTransform.position) / 2) + _curveStart;
            }
            _curveEnd = endTransform.position;
            if (Utilities.IsValid(gates[gateIndex + 1].reverseControlPoint))
            {
                _curveEndDirection = gates[gateIndex + 1].reverseControlPoint.position;
            }
            else
            {
                _curveEndDirection = (endTransform.forward * -1 * Vector3.Distance(startTransform.position, endTransform.position) / 2) + _curveEnd;
            }
            
            
            for (int resolutionIndex = 0; resolutionIndex < curveResolution; resolutionIndex++)
            {
                float time = resolutionIndex / (float)curveResolution;
                _curve[curveIndex] = GetPointAtTime(time);
                curveIndex++;
            }
        }

        //guideLine.positionCount = _curve.Length;
        //guideLine.SetPositions(_curve);
    }

    private int _pointOnCurve;
    public void DrawCurveProgressive()
    {
        Vector3[] tempCurve = new Vector3[curveResolution];
        for (int i = 0; i < curveResolution; i++)
        {
            int index = (_pointOnCurve + i) % _curve.Length;
            tempCurve[i] = _curve[index];
        }

        guideLine.positionCount = tempCurve.Length;
        guideLine.SetPositions(tempCurve);
    }

    // 0.0 >= t <= 1.0 In here be dragons and magic
    public Vector3 GetPointAtTime(float t)
    {
        float u = 1f - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        Vector3 p = uuu * _curveStart; //first term
        p += 3 * uu * t * _curveStartDirection; //second term
        p += 3 * u * tt * _curveEndDirection; //third term
        p += ttt * _curveEnd; //fourth term

        return p;
    }
}
