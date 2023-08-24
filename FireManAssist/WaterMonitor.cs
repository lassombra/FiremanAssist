using DV.HUD;
using DV.Simulation.Cars;
using LocoSim.Implementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FireManAssist
{
    public class WaterMonitor : MonoBehaviour
    {
        // The water level as handed to the sight glass
        private Port waterPort;
        // The fire port, to determine if the fire is on
        private Port firePort;

        // The injector port, to set the injector
        private Port injector;
        // The blowdown port, to turn off blowdown if on at minimum
        private Port blowdown;

        // run counter and SKIP_TICKS are used to reduce CPU load by only running once SKIP_TICKS has elapsed
        private int runCounter = 0;
        private static readonly int SKIP_TICKS = 5;

        // Whether or not the "full" mode is actively running
        // provided mod settings allow "full" mode, this will be true
        // whenever the fire is on, and will remain true for one more update after the fire is turned off
        // that update may be delayed by the SKIP_TICKS counter
        private bool running = false;

        // true if an override has triggered, turned false by over/under protection which can reenable full service
        private bool overrideTriggered = false;
        // the last set injector value, used to determine if the injector has been manually set
        private float lastSetInjector = -1.0f;

        public void Start()
        {
            var trainCar = this.GetComponentInParent<TrainCar>();
            if (null == trainCar)
            {
                Destroy(this);
                FireManAssist.Logger.Log("No TrainCar found");
                return;
            }
            var simController = trainCar.GetComponent<SimController>();
            if (null == simController)
            {
                Destroy(this);
                FireManAssist.Logger.Log("No SimController found");
                return;
            }
            if (!simController.SimulationFlow.TryGetPort("boiler.WATER_LEVEL_NORMALIZED", out this.waterPort))
            {
                Destroy(this);
                FireManAssist.Logger.Log("Water port not found");
                return;
            }
            simController.SimulationFlow.TryGetPort("firebox.FIRE_ON", out this.firePort);
            simController.SimulationFlow.TryGetPort("injector.EXT_IN", out this.injector);
            simController.SimulationFlow.TryGetPort("blowdown.EXT_IN", out this.blowdown);
        }
        public void Update()
        {
            // skip most of the time, to reduce CPU load
            if (runCounter < SKIP_TICKS || FireManAssist.Settings.WaterMode == WaterAssistMode.None)
            {
                runCounter++;
                return;
            }
            runCounter = 0;
            if (lastSetInjector >= 0.0f && Math.Round(injector.Value, 1) != Math.Round(lastSetInjector, 1))
            {
                // injector has been manually set, disable full service
                running = false;
                overrideTriggered = true;
            }
            float injectorTarget = -1.0f;
            float waterLevel = waterPort.Value;
            switch (FireManAssist.Settings.WaterMode)
            {
                case WaterAssistMode.Full:
                    injectorTarget = FullHandler(waterLevel);
                    goto case WaterAssistMode.Over_Under_Protection;
                case WaterAssistMode.Over_Under_Protection:
                    injectorTarget = OverUnderHandler(waterLevel, injectorTarget);
                    goto case WaterAssistMode.No_Explosions;
                case WaterAssistMode.No_Explosions:
                    injectorTarget = MinimumHandler(waterLevel, injectorTarget);
                    break;
            }
            if (injectorTarget >= 0.0f && injector.Value != injectorTarget)
            {
                injector.ExternalValueUpdate(injectorTarget);
                lastSetInjector = injectorTarget;
            }
        }
        
        /// <summary>
        /// Determines value to set injector to based on water level.
        /// current implementation only operates on the range of 0.75 to 0.85 (0..1 range returned)
        /// </summary>
        /// <param name="waterLevel">Normalized water level as shown in the sight glass</param>
        /// <param name="factor">exponential factor by which to reduce injector level based on current situation, a value of 2.0f square roots the result, providing a gentle but still weighted curve</param>
        /// <returns>Normalized injector target, rounded to 1 decimal place to avoid injector twitching.</returns>
        public static float CalculateInjectorTarget(float waterLevel, float factor = 2.0f)
        {
            const float MaxRange = 0.81667f;
            const float MinRange = 0.75f;
            // baseWaterLevel is current water level minus the bottom of the glass
            // this creates a range of 0.0 to 1.0 (0.75 to 0.85)
            // Min and Max are used to clip the range at [0.0..1.0]
            var normalizedGlassLevel = Math.Max(0.0f, Math.Min(1.0f, ((waterLevel - MinRange) / (MaxRange - MinRange))));

            // target injector level is 1.0 to 0.0 for water level 0.75 to 0.85 or normalized glass level 0.0 to 1.0.
            // however the curve should be weighted towards the lower end of the range by the provided factor
            // so take the result to the power of 1/factor (square root for 2)
            var target = Mathf.Pow(normalizedGlassLevel, 1.0f / factor);
            // and invert it to get the injector level
            target = 1 - target;
            // round to 1 decimal place to avoid injector twitching
            return (float)Math.Round(target, 1);
        }
        private float FullHandler(float waterLevel)
        {
            // Only do full work if fire is on
            // if the fire is out, or override triggered fall back to over/under protection
            if (firePort.Value != 0f && !overrideTriggered)
            {
                running = true;
                // Determine target water level range to determine injector level
                // then set the injector, and then fall through to OverUnderHandler
                return CalculateInjectorTarget(waterLevel);
            } else
            {
                // we had a fire, now we don't, initially turn off the injector.  User can turn it back on (to prime for example) but otherwise we'll just fall through to OverUnderHandler
                if (running)
                {
                    running = false;
                    return 0.0f;
                }
            }
            return -1.0f;
        }
        private float OverUnderHandler(float waterLevel, float injectorTarget)
        {
            // If water level is above 0.85, turn off injector.
            // otherwise fall through to MinimumHandler
            if (waterLevel > 0.85f)
            {
                overrideTriggered = false;
                return 0.0f;
            }
            return injectorTarget;
        }
        private float MinimumHandler(float waterLevel, float injectorTarget)
        {
            // If water level is below 0.75, turn off blowdown.
            // if fire is on also, turn on injector
            if (waterLevel < 0.75f)
            {
                blowdown.ExternalValueUpdate(0f);
                if (firePort.Value == 1f)
                {
                    // override will only reset if the fire is on
                    overrideTriggered = false;
                    return 1.0f;
                }
            }
            return injectorTarget;
        }
    }
}
