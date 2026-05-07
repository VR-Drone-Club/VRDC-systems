using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

public class DronePositionLogger : UdonSharpBehaviour
{
    private DataList _players = new DataList();
    private DataList _output = new DataList();
    private ColorPicker _colorPicker;
    void Start()
    {
        _colorPicker = ColorPicker.Instance();
        PeriodicUpdate();
    }

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        _players.Add(new DataToken(player));
    }

    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        _players.Remove(new DataToken(player));
    }

    public void PeriodicUpdate()
    {
        SendCustomEventDelayedSeconds(nameof(PeriodicUpdate), 1);
        _output.Clear();
        for (int i = 0; i < _players.Count; i++)
        {
            VRCPlayerApi player = (VRCPlayerApi)_players[i].Reference;
            if (!player.GetDrone().IsDeployed()) continue;
            DataList entry = new DataList();
            entry.Add(player.displayName);
            entry.Add(PositionToList(player.GetDrone().GetPosition()));
            entry.Add(ColorToList(_colorPicker.GetEffect(player)));
            _output.Add(entry);
        }

        VRCJson.TrySerializeToJson(_output, JsonExportType.Minify, out DataToken result);
        Debug.Log($"DroneTracker @{result}@");
    }
    
    private DataList ColorToList(Color color)
    {
        DataList output = new DataList();
        output.Add(color.r);
        output.Add(color.g);
        output.Add(color.b);
        output.Add(color.a);
        return output;
    }
    
    private DataList PositionToList(Vector3 position)
    {
        DataList output = new DataList();
        output.Add(position.x);
        output.Add(position.y);
        output.Add(position.z);
        return output;
    }
}