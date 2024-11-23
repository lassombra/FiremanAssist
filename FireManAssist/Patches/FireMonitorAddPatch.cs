using DV;
using DV.Simulation.Cars;
using DV.Simulation.Controllers;
using FireManAssist.Manager;
using HarmonyLib;
using LocoSim.Definitions;
using LocoSim.Implementations;
using LocoSim.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
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
                    var go = new GameObject("fireman");
                    go.transform.parent = boiler.transform.parent;
                    var definition = go.AddComponent<FireMonitorDefinition>();
                    definition.ID = "fire_monitor";
                    definition.boiler = boiler; ;
                    definition.shoveling = shoveling;
                    definition.steamExhaust = steamExhaust;
                    //CreateFiremanResource(prefab, go, definition);
                    ConfigurePortReferences(prefab, definition);
                    var controller = prefab.GetComponentInChildren<SimController>();
                    var modeController = go.AddComponent<FireModeController>();
                    controller.otherSimControllers = controller.otherSimControllers.AddToArray(modeController);
                    go.AddComponent<WaterMonitor>();
                }
            });
        }

        //private static void CreateFiremanResource(GameObject prefab, GameObject go, FireMonitorDefinition definition)
        //{
        //    var controller = prefab.GetComponentInChildren<EnvironmentDamageController>();
        //    var damager = go.AddComponent<EnvironmentDamager>();
        //    damager.damagerPortId = PortHelpers.MakePortId(definition, definition.usage);
        //    damager.environmentDamageResource = (DV.ThingTypes.ResourceType)(ResourceType)150;
        //    go.transform.parent = controller.transform;
        //    controller.entries = controller.entries.AddToArray(damager);
        //}

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
                waterPort = PortHelpers.MakePortId(onBoardWater, onBoardWater.normalizedReadOut);
            } else if (tenderWater != null)
            {
                waterPort = tenderWater.consumerPortId;
            }
            var newDefinitions = new PortReferenceConnection[]
            {
                new PortReferenceConnection(PortHelpers.MakePortId(definition, definition.damperIn), PortHelpers.getExistingConnection(steamExhaustDefinition, steamExhaustDefinition.damperControl, connections)),
                new PortReferenceConnection(PortHelpers.MakePortId(definition, definition.ignition), PortHelpers.MakePortId(fireboxDefinition, fireboxDefinition.ignitionExtIn)),
                new PortReferenceConnection(PortHelpers.MakePortId(definition, definition.blowerIn), PortHelpers.getExistingConnection(steamExhaustDefinition, steamExhaustDefinition.blowerControl, connections)),
                new PortReferenceConnection(PortHelpers.MakePortId(definition, definition.airflow), PortHelpers.MakePortId(steamExhaustDefinition, steamExhaustDefinition.airFlowReadOut)),
                new PortReferenceConnection(PortHelpers.MakePortId(definition, definition.boilerPressure), PortHelpers.MakePortId(boiler, boiler.pressureReadOut)),
                new PortReferenceConnection(PortHelpers.MakePortId(definition, definition.boilerWaterLevel), PortHelpers.MakePortId(boiler, boiler.waterLevelReadOut)),
                new PortReferenceConnection(PortHelpers.MakePortId(definition, definition.fireboxTemp), PortHelpers.MakePortId(fireboxDefinition, fireboxDefinition.temperatureReadOut)),
                new PortReferenceConnection(PortHelpers.MakePortId(definition, definition.firePort), PortHelpers.MakePortId(fireboxDefinition, fireboxDefinition.fireOnReadOut)),
                new PortReferenceConnection(PortHelpers.MakePortId(definition, definition.coalLevel), PortHelpers.MakePortId(fireboxDefinition, fireboxDefinition.coalLevelReadOut)),
                new PortReferenceConnection(PortHelpers.MakePortId(definition, definition.coalCapacity), PortHelpers.MakePortId(fireboxDefinition, fireboxDefinition.coalCapacityReadOut)),
                new PortReferenceConnection(PortHelpers.MakePortId(definition, definition.waterNormalized), waterPort ?? ""),
            };
            connections.executionOrder = connections.executionOrder.AddItem(definition).ToArray();
            connections.portReferenceConnections = connections.portReferenceConnections.AddRangeToArray(newDefinitions);
        }
    }
}
