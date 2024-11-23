using LocoSim.Definitions;
using System.Linq;

namespace FireManAssist.Patches
{
    internal static class PortHelpers
    {
        public static string getExistingConnection(SimComponentDefinition definition, PortReferenceDefinition portReferenceDefinition, SimConnectionDefinition connections)
        {
            return (from p in connections.portReferenceConnections
                    where p.portReferenceId == MakePortId(definition, portReferenceDefinition)
                    select p).FirstOrDefault()?.portId;
        }
        public static string MakePortId(SimComponentDefinition definition, PortDefinition portDefinition)
        {
            return definition.ID + "." + portDefinition.ID;
        }
        public static string MakePortId(SimComponentDefinition definition, PortReferenceDefinition portReferenceDefinition)
        {
            return definition.ID + "." + portReferenceDefinition.ID;
        }
    }
}