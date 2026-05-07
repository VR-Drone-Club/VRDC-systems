using VRC.SDK3.Data;

namespace VRDC_systems.Building.Scripts.Building
{
    public class PropConnection : DataDictionary
    {
        public static PropConnection Create(WorldPropTemplate propTemplate)
        {
#if UNITY_EDITOR && !COMPILER_UDONSHARP
            PropConnection connection = new PropConnection(); // in regular C# we just create the class directly
#else
        PropConnection connection = (PropConnection)new DataDictionary(); // In U# we create a DataList and cast it to the class
#endif
            connection["prop"] = propTemplate;
            connection["propid"] = propTemplate.GetUUID();
            return connection;
        }
    }

    public static class PropConnectionExtensions
    {
        public static PropConnection AsPropConnection(this DataToken token)
        {
            return (PropConnection)token.DataDictionary;
        }
        public static void ClearProp(this PropConnection propConnection)
        {
            propConnection.Remove("prop"); // clear cache so it can serialize
        }
        public static WorldPropTemplate GetProp(this PropConnection propConnection, BuildManager buildManager)
        {
            if (propConnection.ContainsKey("prop") && !propConnection["prop"].IsNull) return (WorldPropTemplate)propConnection["prop"].Reference; // if cached, return
            if (propConnection.ContainsKey("propid"))
            {
                WorldPropTemplate prop = buildManager.GetPropByUUID(propConnection["propid"].String); // search by id
                propConnection["prop"] = prop; // put in cache
                return prop;
            }

            return null;
        }

        public static string GetPropUUID(this PropConnection propConnection)
        {
            return propConnection["propid"].String;
        }
    }
}