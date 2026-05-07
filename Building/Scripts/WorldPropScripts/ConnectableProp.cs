using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRDC_systems.Building.Scripts.Building;

namespace WorldPropScripts
{
    /// <summary>
    /// Connectable props have all the same functionality as normal props, plus they can be connected to other props
    /// </summary>
    public class ConnectableProp : WorldPropTemplate
    {
        public Observable Connections
        {
            get => _connections.AsObservable();
            set => _connections = value;
        }

        private DataList _connections;
        private bool _initialized;

        private void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            Connections = Observable.Create(new DataList());
        }

        public override DataDictionary SerializeProp()
        {
            Initialize();
            DataList connections = Connections.GetList();
            for (int i = 0; i < connections.Count; i++)
            {
                PropConnection connection = (PropConnection)connections[i].DataDictionary;
                connection.ClearProp();
            }
            
            
            currentParameters["connections"] = connections;
            return base.SerializeProp();
        }

        public override void DeserializeProp(DataDictionary parameters)
        {
            Initialize();
            Connections.SetValue(parameters["connections"].DataList);
            base.DeserializeProp(parameters);
        }

        public virtual PropConnection FindConnection(GameObject target)
        {
            Initialize();
            return null;
        }

        public void AddConnection(PropConnection propConnection)
        {
            Initialize();
            Connections.GetList().Add(propConnection);
            BuildManager.RequestSerialization();
            ConnectionChanged();
        }

        public void RemoveConnection(PropConnection propConnection)
        {
            Initialize();
            Connections.GetList().Remove(propConnection);
            BuildManager.RequestSerialization();
            ConnectionChanged();
        }

        public virtual void ConnectionChanged()
        {
            
        }

        public PropConnection GetConnection(int index)
        {
            Initialize();
            return (PropConnection)Connections.GetList()[index].DataDictionary;
        }
    }
}