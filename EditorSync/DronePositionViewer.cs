
#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.Serialization;
using VRC.SDK3.Data;

public class DronePositionViewer : MonoBehaviour
{
    private Dictionary<string, (List<Vector3> positions, Color color)> _records = new Dictionary<string, (List<Vector3> positions, Color color)>();
    public static DronePositionViewer instance;

    private void OnDrawGizmos()
    {
        instance = this;
    }

    private void OnDrawGizmosSelected()
    {
        foreach (var record in _records)
        {
            Gizmos.color = record.Value.color;
            List<Vector3> positions = record.Value.positions;
           for (int i = 0; i + 1 < positions.Count; i++)
            {
                Debug.Log(positions[i]);
                Gizmos.DrawLine(positions[i], positions[i + 1]);
            }
            Gizmos.DrawSphere(positions[0], 0.2f);
        }
    }

    public void AddEntry(string name, Color color, Vector3 position)
    {
        if (!_records.ContainsKey(name))
        {
            List<Vector3> list = new List<Vector3>();
            list.Add(position);
            _records[name] = (list, color);
        }
        else
        {
            List<Vector3> list = _records[name].positions;
            list.Insert(0, position);
            if (list.Count > 5)
            {
                list.RemoveAt(5);
            }
        }
    }
    public static void AddRecords(string message)
    {
        DronePositionViewer instance = DronePositionViewer.instance;
        if (!instance) return;
        if (!message.Contains("DroneTracker")) return;
        int start = message.IndexOf("@");
        int end = message.LastIndexOf("@");
        string extract = message.Substring(start + 1, end - start - 1);
        if (VRCJson.TryDeserializeFromJson(extract, out DataToken result))
        {
            for (int i = 0; i < result.DataList.Count; i++)
            {
                DataList entry = result.DataList[i].DataList;
                string name = entry[0].String;
                Vector3 position = ListToVector(entry[1].DataList);
                Color color = ListToColor(entry[2].DataList);
                instance.AddEntry(name, color, position);
            }
        }
        else
        {
            Debug.Log($"Failed to parse {extract} {result.ToString()}");
        }

        //if (start == -1 || end == -1) return;
    }

    static Vector3 ListToVector(DataList list)
    {
        return new Vector3((float)list[0].Number, (float)list[1].Number, (float)list[2].Number);
    }

    static Color ListToColor(DataList list)
    {
        return new Color((float)list[0].Number, (float)list[1].Number, (float)list[2].Number, (float)list[3].Number);
    }
}
#endif