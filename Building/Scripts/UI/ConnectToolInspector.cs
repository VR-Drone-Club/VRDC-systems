
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;
using VRDC_systems.Building.Scripts.Building;
using VRDC_systems.Building.Scripts.UI;
using WorldPropScripts;

public class ConnectToolInspector : ToolInspector
{
    private ConnectionEditor[] _connectionEditors;
    private bool _initialized;
    
    private void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        _connectionEditors = GetComponentsInChildren<ConnectionEditor>(true);
    }
    public override string AssociatedTool()
    {
        return "ConnectTool";
    }

    public override void SetData(BuildManager buildManager, Builder desktopBuilder, BuilderTool tool)
    {
        Initialize();
        ConnectionTool connectionTool = (ConnectionTool)tool;
        ConnectableProp connectingProp = connectionTool.ConnectingProp;
        if (!Utilities.IsValid(connectingProp)) return;
        DataList connections = connectingProp.Connections.GetList();
        for (int i = 0; i < _connectionEditors.Length; i++)
        {
            if (i < connections.Count)
            {
                _connectionEditors[i].SetData(true, connections[i].AsPropConnection().GetProp(buildManager).name, null);
            }
            else
            {
                _connectionEditors[i].SetData(false);
            }
        }
    }
}
