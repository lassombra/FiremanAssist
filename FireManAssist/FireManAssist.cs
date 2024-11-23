using CommsRadioAPI;
using DV;
using DV.ThingTypes;
using FireManAssist.Radio;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using UnityModManagerNet;
using static UnityModManagerNet.UnityModManager.ModEntry;

namespace FireManAssist
{
    public class FireManAssist
    {
        internal static ModLogger Logger { get; private set; }
        internal static Settings Settings { get; private set; }
        internal static CommsRadioMode CommsRadioMode { get; private set; }
        public static LayerMask TrainCarMask { get; private set; }
        public static LayerMask TrainInteriorMask { get; private set; }
        public static ResourceType_v2 firemanActiveResource { get; private set; }
        static bool Load(UnityModManager.ModEntry modEntry)
        {
            modEntry.OnToggle = OnToggle;
            Settings = Settings.Load<Settings>(modEntry);
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;
            ControllerAPI.Ready += InitCommRadio;
            CreateResource();
            return true;
        }

        private static void CreateResource()
        {
            var resource = new ResourceType_v2();
            resource.isConsumable = false;
            resource.isTaxable = false;
            resource.v1 = (ResourceType)150;
            resource.canBeDamaged = false;
            resource.canDamageEnvironment = false;
            resource.price = 0.83f;
            resource.resourceIcon = Globals.G.Types.ResourceType_to_v2[ResourceType.Coal].resourceIcon;
            resource.name = "Fireman";
            resource.id = "resource_fireman";
            Globals.G.Types.resources.Add(resource);
            Globals.G.Types.RecalculateCaches();
            firemanActiveResource = resource;
        }

        internal static void InitCommRadio()
        {
            CommsRadioMode = CommsRadioMode.Create(new RadioSelectBehaviour(), laserColor: new Color(0.8f, 0.333f, 0f));
            TrainCarMask = LayerMask.GetMask(new string[]
            {
                "Train_Big_Collider"
            });
            TrainInteriorMask = LayerMask.GetMask(new string[]
            {
                "Train_Interior"
            });
        }


        public static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            Settings.Draw(modEntry);
        }
        public static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            Settings.Save(modEntry);
        }

        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            if (value)
            {
                Logger = modEntry.Logger;
                var harmony = new Harmony(modEntry.Info.Id);
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            } 
            else
            {
            }
            return true;
        }
        /// <summary>
        /// Determines an interval output based on an input value, a factor, and a range.
        /// </summary>
        /// <param name="referenceValue">Normalized reference value</param>
        /// <param name="factor">exponential factor by which to reduce output level based on current situation, a value of 2.0f square roots the result, providing a gentle but still weighted curve</param>
        /// <param name="maxRange">Maximum input to allow , default 0.85, at or above this level the output will be 0</param>
        /// <param name="minRange">Minimum input level to allow, default 0.75, at or below this level the output will be 1</param>
        /// <param name="interval">Interval to round to, defaults to 0.1f</param>
        /// <returns>Normalized injector target, rounded to 1 decimal place to avoid injector twitching.</returns>
        public static float CalculateIntervalFromCurve(float referenceValue, float factor = 2.0f, float minRange = 0.75f, float maxRange = 0.81667f, float interval=0.1f)
        {
            // baseWaterLevel is current water level minus the bottom of the glass
            // this creates a range of 0.0 to 1.0 (0.75 to 0.85)
            // Min and Max are used to clip the range at [0.0..1.0]
            var normalizedGlassLevel = Math.Max(0.0f, Math.Min(1.0f, ((referenceValue - minRange) / (maxRange- minRange))));

            // target injector level is 1.0 to 0.0 for water level 0.75 to 0.85 or normalized glass level 0.0 to 1.0.
            // however the curve should be weighted towards the lower end of the range by the provided factor
            // so take the result to the power of 1/factor (square root for 2)
            var target = Mathf.Pow(normalizedGlassLevel, 1.0f / factor);
            // and invert it to get the injector level
            target = 1 - target;
            // pin to nearest multiple of interval
            target = Mathf.Round(target / interval) * interval;
            return target;
        }
    }
}
