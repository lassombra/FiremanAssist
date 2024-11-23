using DV.HUD;
using DV.Simulation.Cars;
using FireManAssist.Manager;
using LocoSim.Definitions;
using LocoSim.Implementations;
using System;
using UnityEngine;

namespace FireManAssist
{
    public enum WaterCurve
    {
        // Normal operating range
        Default,
        // Pressure is dropping, or has dropped below 12 bar
        LowPressure,
        // pressure is nearly at maximum
        HighPressure,
        // pressure is just building up, let the fire warm up first
        Startup
    }

    public class WaterMonitor : AbstractInfrequentUpdateComponent
    {
        // The water level as handed to the sight glass
        private Port waterPort;
        // The fire port, to determine if the fire is on
        private Port firePort;

        // The injector port, to set the injector
        private Port injector;
        // The blowdown port, to turn off blowdown if on at minimum
        private Port blowdown;

        // The boiler pressure port, used to monitor the boiler pressure and adjust the injector curves accordingly
        private Port boilerPressure;

        // The angle of the water in the boiler, used to determine the true water level
        private Port angleExtIn;

        // The amount of water in the cylinders
        private Port cylinderWater;
        private Port cylinderTemperature;

        //Control to open / close the cylinder cocks
        private Port cylinderCocks;


        // Whether or not the "full" mode is actively running
        // provided mod settings allow "full" mode, this will be true
        // whenever the fire is on, and will remain true for one more update after the fire is turned off
        // that update may be delayed by the SKIP_TICKS counter
        private bool running = false;

        // true if an override has triggered, turned false by over/under protection which can reenable full service
        private bool overrideTriggered = false;
        // the last set injector value, used to determine if the injector has been manually set
        private float lastSetInjector = -1.0f;

        // Whether or not we've gone into low pressure mode.  This mode is triggered by a dropping pressure trend while under 13bar and will not be exited until the pressure is above 13bar
        private bool lowPressure = false;
        private bool highPressure = false;
        public FireModeController FireMonitor { get; set; }
        private readonly PressureTracker pressureTracker = new PressureTracker();

        private ReciprocatingSteamEngineDefinition engineDefinition;
        private BoilerDefinition boilerDefinition;

        private float minWater;
        private float lastCylinderWater = 0.0f;
        private float aspectRatio;

        public Single WaterLevel => waterPort.Value;

        protected override void Init()
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
            engineDefinition = trainCar.GetComponentInChildren<ReciprocatingSteamEngineDefinition>();
            if (null == engineDefinition)
            {
                Destroy(this);
                FireManAssist.Logger.Log("No reciprocating engine definition found");
                return;
            }
            boilerDefinition = trainCar.GetComponentInChildren<BoilerDefinition>();
            if (null == boilerDefinition)
            {
                Destroy(this);
                FireManAssist.Logger.Log("Boiler definition not found");
                return;
            }
            FireMonitor = trainCar.GetComponentInChildren<FireModeController>();
            minWater = boilerDefinition.crownSheetNormalizedWaterLevel;
            simController.SimulationFlow.TryGetPort("firebox.FIRE_ON", out this.firePort);
            simController.SimulationFlow.TryGetPort("injector.EXT_IN", out this.injector);
            simController.SimulationFlow.TryGetPort("blowdown.EXT_IN", out this.blowdown);
            simController.SimulationFlow.TryGetPort(boilerDefinition.ID + "." + boilerDefinition.pressureReadOut.ID, out this.boilerPressure);
            simController.SimulationFlow.TryGetPort("boiler.BOILER_ANGLE_EXT_IN", out this.angleExtIn);
            simController.SimulationFlow.TryGetPort("cylinderCock.EXT_IN", out this.cylinderCocks);
            simController.SimulationFlow.TryGetPort(engineDefinition.ID + "." + engineDefinition.cylinderTemperatureReadOut.ID, out this.cylinderTemperature);
            simController.SimulationFlow.TryGetPort(engineDefinition.ID + "." + engineDefinition.waterInCylindersNormalizedReadOut.ID, out this.cylinderWater);
            aspectRatio = boilerDefinition.length / boilerDefinition.diameter;
            
        }
        public override void Update()
        {
            if (FireMonitor.Mode != Mode.Dismissed)
            {
                if (lastSetInjector >= 0.0f && Math.Round(injector.Value, 1) != Math.Round(lastSetInjector, 1) && FireManAssist.Settings.InjectorMode == InjectorOverrideMode.Complete)
                {
                    // injector has been manually set, disable full service
                    running = false;
                    overrideTriggered = true;
                }
                if (FireManAssist.Settings.InjectorMode == InjectorOverrideMode.None && lastSetInjector >= 0.0f &&
                    (firePort.Value > 0.0f || FireMonitor.Firing))
                {
                    injector.ExternalValueUpdate(lastSetInjector);
                }
            }
            base.Update();
        }
        protected override void InfrequentUpdate(bool slowUpdateFrame)
        {
            if (FireMonitor.Mode == Mode.Dismissed)
            {
                return;
            }
            float injectorTarget = -1.0f;
            float waterLevel = waterPort.Value;
            float correction = this.aspectRatio / 2f * Mathf.Tan(-this.angleExtIn.Value * 3.1415927f / 180f);
            // if correction is negative (water level in the sight glass is below actual water level in the boiler)
            // then use the displayed water level, otherwise use the true water level.
            waterLevel = Math.Min(waterLevel, waterLevel - correction);
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
            MaybeUpdateInjector(injectorTarget, waterLevel, slowUpdateFrame);
            if (slowUpdateFrame && FireManAssist.Settings.AutoCylinderCocks)
            {
                UpdateCylinderCocks();
            }
        }

        private void UpdateCylinderCocks()
        {
            var rate = engineDefinition.maxWaterExpulsionRate;
            // Open if we have at least half of a normal max clear amount of water, temp is below 25 degrees celsius (been idle for a while) or water is climbing despite the expulsion
            if (cylinderWater.Value > (engineDefinition.maxWaterExpulsionRate * 0.5f) || cylinderTemperature.Value < 25f || (cylinderWater.Value >= lastCylinderWater && cylinderWater.Value > 0.0f))
            {
                cylinderCocks.ExternalValueUpdate(1.0f);
            }
            // Close if temp is over 100 degrees celsius (water won't condense anymore, and will boil off) or we're over 25 degrees celsius (we're warmish) and there's no water left.
            else if (cylinderTemperature.Value >= 100f ||  (cylinderWater.Value <= 0.0f && cylinderTemperature.Value > 25.0f))
            {
                cylinderCocks.ExternalValueUpdate(0.0f);
            }
            lastCylinderWater = cylinderWater.Value;
        }

        private void MaybeUpdateInjector(float injectorTarget, float waterLevel, bool slowUpdateFrame)
        {
            // We want to set injector in the following cases:
            // 1) We have a change AND we're not in override mode OR Override rules are not set to Complete AND we're on a slowUpdateFrame
            // 2) We're below 75% water and the target is above 0,
            // 3) we're above 85% water.
            bool updateInjector = false;
            updateInjector = updateInjector || (FireManAssist.Settings.InjectorMode == InjectorOverrideMode.None && injectorTarget >= 0.0f);
            updateInjector = updateInjector || (injectorTarget >= 0.0f && lastSetInjector != injectorTarget);
            updateInjector = updateInjector && slowUpdateFrame;
            updateInjector = updateInjector || (minWater > waterLevel && injectorTarget > 0.0f);
            updateInjector = updateInjector || (minWater+0.1f < waterLevel);
            updateInjector = updateInjector && (firePort.Value > 0.0f || FireMonitor.Firing || waterLevel >= minWater + 0.05f);
            if (updateInjector)
            {
                injector.ExternalValueUpdate(injectorTarget);
                lastSetInjector = injectorTarget;
            }
        }

        /// <summary>
        /// Takes in the current water level and a target curve to determine the goal injector value
        /// Also provides a great single place to tune the injector curves.
        /// </summary>
        /// <param name="curve">The curve to use to determine the injector target</param>
        /// <param name="waterLevel">The current water level</param>
        public static float CalculateInjectorTargetCurve(float waterLevel, WaterCurve curve, float minWater)
        {
            var min = minWater + 0.04f;
            var low = min + 0.01667f;
            var max = minWater + 0.1f;
            var nominal = minWater + 0.06667f;
            switch (curve)
            {
                case WaterCurve.LowPressure:
                    return CalculateInjectorTarget(waterLevel, 4.0f, min, nominal);
                case WaterCurve.HighPressure:
                    return CalculateInjectorTarget(waterLevel, 1 / 3.0f, nominal, max);
                case WaterCurve.Startup:
                    return CalculateInjectorTarget(waterLevel, 4.0f, min, low);
                case WaterCurve.Default:
                default:
                    return CalculateInjectorTarget(waterLevel, 2.0f, min, nominal);
            }
        }

        /// <summary>
        /// Determines value to set injector to based on water level.
        /// current implementation only operates on the range of 0.75 to 0.85 (0..1 range returned)
        /// </summary>
        /// <param name="waterLevel">Normalized water level as shown in the sight glass</param>
        /// <param name="factor">exponential factor by which to reduce injector level based on current situation, a value of 2.0f square roots the result, providing a gentle but still weighted curve</param>
        /// <param name="maxRange">Maximum water level to allow, default 0.85, at or above this level the injector will be fully closed</param>
        /// <param name="minRange">Minimum water level to allow, default 0.75, at or below this level the injector will be fully open</param>
        /// <returns>Normalized injector target, rounded to 1 decimal place to avoid injector twitching.</returns>
        public static float CalculateInjectorTarget(float waterLevel, float factor = 2.0f, float minRange = 0.75f, float maxRange = 0.81667f)
        {
            // baseWaterLevel is current water level minus the bottom of the glass
            // this creates a range of 0.0 to 1.0 (0.75 to 0.85)
            // Min and Max are used to clip the range at [0.0..1.0]
            var normalizedGlassLevel = Math.Max(0.0f, Math.Min(1.0f, ((waterLevel - minRange) / (maxRange- minRange))));

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
                // pressure is reported as 1-19, but we want 0-18 (0-18 bar);
                var pressure = boilerPressure.Value;
                var trends = pressureTracker.UpdateAndCheckTrend(pressure);
                var curve = WaterCurve.Default;
                // rule sequence is:
                // 1) If boiler is below 12 bar, use low pressure curve,
                // 2) If boiler is below 13 bar and above 12 bar, check pressure trend - if pressure is falling, or our lowPressure flag is set, use low pressure curve
                // 3) If boiler is above 13 bar, reset lowPressure flag
                // 4) If boiler is above 14 bar, use high pressure curve
                // 5) if all else fails, use default curve
                if (pressure < boilerDefinition.safetyValveOpeningPressure - 5f)
                {
                    curve = WaterCurve.Startup;
                }
                else if (pressure < boilerDefinition.safetyValveOpeningPressure - 2.0f)
                {
                    highPressure = false;
                    curve = WaterCurve.LowPressure;
                }
                else if (pressure < boilerDefinition.safetyValveOpeningPressure - 1.0f)
                {
                    if (trends.longtermTrend == Trend.Falling || lowPressure)
                    {
                        curve = WaterCurve.LowPressure;
                        lowPressure = true;
                        highPressure = false;
                    }
                }
                else if (pressure > boilerDefinition.safetyValveOpeningPressure - 0.2f)
                {
                    curve = WaterCurve.HighPressure;
                    lowPressure = false;
                    highPressure = true;
                } else if (highPressure && pressure >= boilerDefinition.safetyValveOpeningPressure - 0.5f)
                {
                    curve = WaterCurve.HighPressure;
                } else
                {
                    curve = WaterCurve.Default;
                    lowPressure = false;
                    highPressure = false;
                }
                return CalculateInjectorTargetCurve(waterLevel, curve, minWater);
            } else
            {
                // we had a fire, now we don't, initially turn off the injector.  User can turn it back on (to prime for example) but otherwise we'll just fall through to OverUnderHandler
                if (running || FireManAssist.Settings.InjectorMode == InjectorOverrideMode.None)
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
            if (waterLevel > minWater + 0.1f)
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
            if (waterLevel < minWater)
            {
                blowdown.ExternalValueUpdate(0f);
                if (firePort.Value == 1f || FireMonitor.Firing)
                {
                    // override will only reset if the fire is on or the fireman is trying to start it
                    overrideTriggered = false;
                    return 1.0f;
                }
            }
            return injectorTarget;
        }
    }
}
