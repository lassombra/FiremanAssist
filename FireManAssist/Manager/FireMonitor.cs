using DV.Simulation.Cars;
using DV.Simulation.Controllers;
using LocoSim.Definitions;
using LocoSim.Implementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FireManAssist
{
    public enum State
    {
        Off,
        ShuttingDown,
        Running,
        WaterOut
    }
    internal class FireMonitor : AbstractInfrequentUpdateComponent
    {
        // Control for damper
        private Port damperIn;
        // control for blower
        private Port blowerIn;
        // current airflow through the firebox - this is used instead of forward speed because it takes into account more variables
        // a real fireman would have a pretty good idea of what this would be in a given situation, so it's a pretty good proxy for "locomotive state"
        // when low and pressure is low, then the blower should be on
        private Port airflow;

        // The boiler pressure port, used to monitor the boiler pressure and adjust the damper curves accordingly
        private Port boilerPressure;
        private WaterMonitor WaterMonitor;

        // this is the firebox itself - it's a useful object as it has a lot of the variables we need
        // and it's guaranteed to be present for the two locomotives we're supporting
        private FireboxSimController fireController;
        // This is how keyboard coal add is simulated.  We use it to add coal to the firebox.
        private MagicShoveling shovelController;

        private Single lastSetDamper;
        private readonly PressureTracker pressureTracker = new PressureTracker();
        private bool firing = false;
        private Port waterCapacity;
        private readonly float minReserve = 0.1f;
        private float maxPressure = 14.5f;
        private BoilerDefinition definition;
        private SteamExhaustDefinition exhaustDefinition;

        public State State { get
            {
                if (firing && SufficientReserve)
                {
                    return State.Running;
                } else if (firing) {
                    return State.WaterOut;
                } else if (FireOn)
                {
                    return State.ShuttingDown;
                } else
                {
                    return State.Off;
                }
            }
        }

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
            definition = trainCar.GetComponentInChildren<BoilerDefinition>();
            if (null == definition)
            {
                Destroy(this);
                FireManAssist.Logger.Log("No Boiler Definition Found");
                return;
            }
            exhaustDefinition = trainCar.GetComponentInChildren<SteamExhaustDefinition>();
            WaterMonitor = trainCar.GetComponent<WaterMonitor>();
            WaterMonitor.Firing = false;
            fireController = trainCar.GetComponent<SimController>().firebox;
            shovelController = trainCar.GetComponent<MagicShoveling>();
            simController.SimulationFlow.TryGetPort("damper.EXT_IN", out this.damperIn);
            simController.SimulationFlow.TryGetPort("blower.EXT_IN", out this.blowerIn);
            simController.SimulationFlow.TryGetPort(definition.ID + "." + definition.pressureReadOut.ID, out this.boilerPressure);
            simController.SimulationFlow.TryGetPort(exhaustDefinition.ID + "." + exhaustDefinition.airFlowReadOut.ID, out this.airflow);
            // TODO: Start going through orderedsimcomps for the components that I want to use - specifically 
            // I need the boiler in order to get it's water consumption port reference so I can get it's water port.
            string waterSource = simController.connectionsDefinition.portReferenceConnections.First(connection => connection.portReferenceId == definition.ID + "." + definition.water.ID)
                .portId.Split('.')[0];
            simController.SimulationFlow.TryGetPort(waterSource + ".NORMALIZED", out this.waterCapacity);
            maxPressure = definition.safetyValveOpeningPressure;
            //Offset from water monitor since these get added in the same tick
            lastUpdate = 3;
        }

        public Single AirFlow => airflow.Value;
        public Single Pressure => boilerPressure.Value;
        public Boolean FireOn => fireController.IsFireOn;
        public Single FireboxContentsNormalized => fireController.FireboxContents / fireController.FireboxCapacity;
        public Single WaterReserve => this.waterCapacity.Value;
        public bool SufficientReserve => this.waterCapacity.Value >= this.minReserve;

        public void ToggleFiring()
        {
            firing = !firing;
            WaterMonitor.Firing = firing;
        }
        private static Single FireboxTarget(Trend trend, Single pressure)
        {
            var target = 0.0f;
            if (FireManAssist.Settings.FireMode == FireAssistMode.Full)
            {
                switch (trend)
                {
                    // minimum coal - it's rising, don't do much
                    case Trend.Rising:
                        target = 0.05f;
                        break;
                    // it's falling - we might need to add a lot of coal, but we'll plan it based on the range of 14.5 to 13.5 (actually 13.5 to 12.5)
                    case Trend.Falling:
                        target = FireManAssist.CalculateIntervalFromCurve(pressure, 3.0f, 13.0f, 14.5f, 0.01f);
                        break;
                    default:
                        // Use a cube root curve based on current pressure - this will ramp down rapidly.
                        target = FireManAssist.CalculateIntervalFromCurve(pressure, 3.0f, 13.5f, 14.5f, 0.01f);
                        break;
                }
            }
            else if (FireManAssist.Settings.FireMode == FireAssistMode.KeepBurning)
            {
                target = 0.05f;
            }
            // Never more than 55% full, and never less than 0% full.  More than 55% doesn't actually efficiently raise pressure, but can waste coal.
            return Math.Min(Math.Max(target, 0.0f), 0.55f);
        }
        private Single Normalize(Single value, Single min, Single max)
        {
            return Math.Max(0.0f, Math.Min(1.0f, (value - min) / (max - min)));
        }
        protected override void InfrequentUpdate(bool slowUpdateFrame)
        {
            var trend = pressureTracker.UpdateAndCheckTrend(Pressure);
            if (firing && FireOn && SufficientReserve && FireManAssist.Settings.FireMode != FireAssistMode.None)
            {
                // It's simple, if pressure isn't rising and we're below target, add coal, make hot.
                bool shouldAddCoal = trend != Trend.Rising && Pressure < (maxPressure - 1.0f);
                if (shouldAddCoal && slowUpdateFrame)
                {
                    shovelController.AddCoalToFirebox(1);
                }
            }
            if (FireOn)
            {
                UpdateDamperAndBlower(slowUpdateFrame);
            }
            else if (firing && SufficientReserve && FireManAssist.Settings.FireMode == FireAssistMode.Full && WaterMonitor.WaterLevel >= 0.75f)
            {
                // get a fire going because we're supposed to be on, but we're not.
                shovelController.AddCoalToFirebox(2);
                fireController.Ignite();
            }
        }

        private void UpdateDamperAndBlower(bool slowUpdateFrame)
        {
            if (FireManAssist.Settings.FiremanManagesBlowerAndDamper && slowUpdateFrame)
            {
                // Up to maxPressure - 1 bar we're still trying to add pressure, so don't limit the fire temp.
                // As we cross that, we want to chill the fire by closing the damper all the way until we are 0.5 bar below safety
                // at which point we want damper full.
                // We prefer to max out damper at this point as coal lasts longer than water, and it's better to waste a bit of coal burning poorly than to
                // waste water popping the safety.
                lastSetDamper = FireManAssist.CalculateIntervalFromCurve(Pressure, 0.5f, maxPressure - 1.0f, maxPressure - 0.5f, 0.2f);
                // If we're still in startup mode, or airflow is abysmal (because we're moving very slowly) then turn on the blower - this is especially useful
                // to get pressure built back up after a hill if the driver closes the throttle which reduces airflow
                if ((Pressure <= (maxPressure - 3.0f) && AirFlow <= (exhaustDefinition.passiveExhaust + exhaustDefinition.maxBlowerFlow)) && lastSetDamper >= 0.99f)
                {
                    blowerIn.ExternalValueUpdate(1.0f);
                }
                // if pressure is high and airflow is still moderate or if airflow has started in earnest, or the damper is 
                // being closed, then we definitely don't want the blower.
                else if (Pressure >= (maxPressure - 1.5f) || AirFlow > (exhaustDefinition.passiveExhaust + (2 * exhaustDefinition.maxBlowerFlow)) || lastSetDamper <0.99f)
                {
                    blowerIn.ExternalValueUpdate(0.0f);
                }

            }
        }
        public override void Update()
        {
            base.Update();
            if (FireOn && FireManAssist.Settings.FiremanManagesBlowerAndDamper)
            {
                damperIn.ExternalValueUpdate(lastSetDamper);
            }
        }
    }
}
