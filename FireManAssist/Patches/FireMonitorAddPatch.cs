using DV;
using DV.Simulation.Controllers;
using FireManAssist.Manager;
using HarmonyLib;
using LocoSim.Definitions;
using LocoSim.Implementations;
using LocoSim.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FireManAssist.Patches
{
    [HarmonyPatch(typeof(CarSpawner), "Awake")]
    public class FireMonitorAddPatch
    {
        static void Prefix()
        {
            FireManAssist.Logger.Log("Parsing Prefabs");
            Globals.G.Types.Liveries.ForEach(type =>
            {
                var prefab = type.prefab;
                if (prefab.GetComponentInChildren<BoilerDefinition>() != null && prefab.GetComponentInChildren<FireMonitorDefinition>() == null)
                {
                    FireManAssist.Logger.Log("Adding fire monitor definition to car " + type.prefab.name);
                    var boiler = prefab.GetComponentInChildren<BoilerDefinition>();
                    var steamExhaust = prefab.GetComponentInChildren<SteamExhaustDefinition>();
                    var shoveling = prefab.GetComponentInChildren<MagicShoveling>();
                    var definition = boiler.transform.parent.gameObject.AddComponent<FireMonitorDefinition>();
                    definition.boiler = boiler; ;
                    definition.shoveling = shoveling;
                    definition.steamExhaust = steamExhaust;

                    ConfigurePortReferences(prefab, definition);
                }
            });
        }

        private static void ConfigurePortReferences(GameObject prefab, FireMonitorDefinition definition)
        {
            var fireboxDefinition = prefab.GetComponentInChildren<FireboxDefinition>();
            var steamExhaustDefinition = definition.steamExhaust;
            var boiler = definition.boiler;
            var connections = prefab.GetComponentInChildren<SimConnectionDefinition>();
            var onBoardWater = prefab.GetComponentInChildren<WaterContainerDefinition>();
            var tenderWater = (from c in prefab.GetComponentsInChildren<BroadcastPortValueConsumer>()
                               where c.connectionTag.ToLower().Contains("tender")
                               where c.connectionTag.ToLower().Contains("normalized")
                               where c.connectionTag.ToLower().Contains("water")
                               select c).FirstOrDefault();
            string waterPort = null;
            if (onBoardWater != null)
            {
                waterPort = MakePortId(onBoardWater, onBoardWater.normalizedReadOut);
            } else if (tenderWater != null)
            {
                waterPort = tenderWater.consumerPortId;
            }
            var newDefinitions = new PortReferenceConnection[]
            {
                new PortReferenceConnection(MakePortId(definition, definition.damperIn), getExistingConnection(fireboxDefinition, fireboxDefinition.damperControl, connections)),
                new PortReferenceConnection(MakePortId(definition, definition.ignition), MakePortId(fireboxDefinition, fireboxDefinition.ignitionExtIn)),
                new PortReferenceConnection(MakePortId(definition, definition.blowerIn), getExistingConnection(steamExhaustDefinition, steamExhaustDefinition.blowerControl, connections)),
                new PortReferenceConnection(MakePortId(definition, definition.airflow), MakePortId(steamExhaustDefinition, steamExhaustDefinition.airFlowReadOut)),
                new PortReferenceConnection(MakePortId(definition, definition.boilerPressure), MakePortId(boiler, boiler.pressureReadOut)),
                new PortReferenceConnection(MakePortId(definition, definition.boilerWaterLevel), MakePortId(boiler, boiler.waterLevelReadOut)),
                new PortReferenceConnection(MakePortId(definition, definition.fireboxTemp), MakePortId(fireboxDefinition, fireboxDefinition.temperatureReadOut)),
                new PortReferenceConnection(MakePortId(definition, definition.firePort), MakePortId(fireboxDefinition, fireboxDefinition.fireOnReadOut)),
                new PortReferenceConnection(MakePortId(definition, definition.coalLevel), MakePortId(fireboxDefinition, fireboxDefinition.coalLevelReadOut)),
                new PortReferenceConnection(MakePortId(definition, definition.coalCapacity), MakePortId(fireboxDefinition, fireboxDefinition.coalCapacityReadOut)),
                new PortReferenceConnection(MakePortId(definition, definition.waterNormalized), waterPort ?? ""),
            };
            connections.executionOrder = connections.executionOrder.AddItem(definition).ToArray();
            connections.portReferenceConnections = connections.portReferenceConnections.AddRangeToArray(newDefinitions);
        }
        private static string getExistingConnection(SimComponentDefinition definition, PortReferenceDefinition portReferenceDefinition, SimConnectionDefinition connections)
        {
            return (from p in connections.portReferenceConnections
             where p.portReferenceId == MakePortId(definition, portReferenceDefinition)
             select p).FirstOrDefault()?.portId;
        }
        private static string MakePortId(SimComponentDefinition definition, PortDefinition portDefinition)
        {
            return definition.ID + "." + portDefinition.ID;
        }
        private static string MakePortId(SimComponentDefinition definition, PortReferenceDefinition portReferenceDefinition)
        {
            return definition.ID + "." + portReferenceDefinition.ID;
        }
    }
}
