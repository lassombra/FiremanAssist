using DV.Simulation.Controllers;
using LocoSim.Definitions;
using LocoSim.Implementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FireManAssist.Manager
{
    public class FireMonitorDefinition : SimComponentDefinition
    {
        public MagicShoveling shoveling;
        public BoilerDefinition boiler;
        public SteamExhaustDefinition steamExhaust;
        public PortReferenceDefinition damperIn = new PortReferenceDefinition(PortValueType.CONTROL, "DAMPER", true);
        // control for blower
        public PortReferenceDefinition blowerIn = new PortReferenceDefinition(PortValueType.CONTROL, "BLOWER", true);
        public PortReferenceDefinition ignition = new PortReferenceDefinition(PortValueType.CONTROL, "IGNITION", true);
        public PortReferenceDefinition airflow = new PortReferenceDefinition(PortValueType.MASS_RATE, "AIR_FLOW", false);
        public PortReferenceDefinition boilerPressure = new PortReferenceDefinition(PortValueType.PRESSURE, "BOILER_PRESSURE", false);
        public PortReferenceDefinition boilerWaterLevel = new PortReferenceDefinition(PortValueType.WATER, "BOILER_WATER", false);
        public PortReferenceDefinition waterNormalized = new PortReferenceDefinition(PortValueType.WATER, "WATER_NORMALIZED", false);
        public PortReferenceDefinition fireboxTemp = new PortReferenceDefinition(PortValueType.TEMPERATURE, "TEMPERATURE", false);
        public PortReferenceDefinition firePort = new PortReferenceDefinition(PortValueType.STATE, "FIRE_ON", false);
        public PortReferenceDefinition coalLevel = new PortReferenceDefinition(PortValueType.COAL, "COAL_LEVEL", false);
        public PortReferenceDefinition coalCapacity = new PortReferenceDefinition(PortValueType.COAL, "COAL_CAPACITY", false);
        public PortDefinition firing = new PortDefinition(PortType.READONLY_OUT, PortValueType.STATE, "FIRING");
        public PortDefinition condition = new PortDefinition(PortType.READONLY_OUT, PortValueType.STATE, "CONDITION");
        public PortDefinition mode = new PortDefinition(PortType.EXTERNAL_IN, PortValueType.CONTROL, "MODE");

        public override SimComponent InstantiateImplementation()
        {
            return new FireMonitor(this);
        }
    }
}
