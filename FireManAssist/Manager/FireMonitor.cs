using DV;
using DV.Simulation.Cars;
using DV.Simulation.Controllers;
using LocoSim.Definitions;
using LocoSim.Implementations;
using System;
using System.Collections;
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
    public enum Mode
    {
        Off,
        Idle,
        Shunt,
        Road,
        Dismissed
    }
    public class FireMonitor : MonoBehaviour
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
        private Port waterCapacity;
        private Port fireboxTemp;
        private readonly float minReserve = 0.1f;
        private float maxPressure = 14.5f;
        private BoilerDefinition definition;
        private SteamExhaustDefinition exhaustDefinition;

        public State State { get
            {
                if (Firing && SufficientReserve)
                {
                    return State.Running;
                } else if (Firing) {
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

        public Mode Mode { get; set; }
        public bool Firing { get
            {
                return Mode != Mode.Off && Mode != Mode.Dismissed;
            }
        }

        protected void Start()
        {
            Mode = Mode.Off;
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
            WaterMonitor.FireMonitor = this;
            fireController = trainCar.GetComponent<SimController>().firebox;
            shovelController = trainCar.GetComponent<MagicShoveling>();
            simController.SimulationFlow.TryGetPort("damper.EXT_IN", out this.damperIn);
            simController.SimulationFlow.TryGetPort("blower.EXT_IN", out this.blowerIn);
            simController.SimulationFlow.TryGetPort(definition.ID + "." + definition.pressureReadOut.ID, out this.boilerPressure);
            simController.SimulationFlow.TryGetPort(exhaustDefinition.ID + "." + exhaustDefinition.airFlowReadOut.ID, out this.airflow);
            var fireboxTempPortId = simController.connectionsDefinition.portReferenceConnections.First(portReferenceConnection => portReferenceConnection.portReferenceId == definition.ID + "." + definition.fireboxTemperature.ID)
                .portId;
            simController.SimulationFlow.TryGetPort(fireboxTempPortId, out this.fireboxTemp);
            // TODO: Start going through orderedsimcomps for the components that I want to use - specifically 
            // I need the boiler in order to get it's water consumption port reference so I can get it's water port.
            string waterSource = simController.connectionsDefinition.portReferenceConnections.First(connection => connection.portReferenceId == definition.ID + "." + definition.water.ID)
                .portId.Split('.')[0];
            simController.SimulationFlow.TryGetPort(waterSource + ".NORMALIZED", out this.waterCapacity);
            maxPressure = definition.safetyValveOpeningPressure;
            StartCoroutine(FiremanUpdate());
        }

        public Single AirFlow => airflow.Value;
        public Single Pressure => boilerPressure.Value;
        public Boolean FireOn => fireController.IsFireOn;
        public Single FireboxContentsNormalized => fireController.FireboxContents / fireController.FireboxCapacity;
        public Single WaterReserve => this.waterCapacity.Value;
        public bool SufficientReserve => this.waterCapacity.Value >= this.minReserve;

        private Single Normalize(Single value, Single min, Single max)
        {
            return Math.Max(0.0f, Math.Min(1.0f, (value - min) / (max - min)));
        }
        protected IEnumerator FiremanUpdate()
        {
            float t_dot = 0.0f;
            float t_ddot = 0.0f;
            float p_dot = 0.0f;
            float p_ddot = 0.0f;
            float lastPressure = Pressure;
            float lastTemperature = fireboxTemp.Value;
            int secondsSinceLastCoal = 0;
            while (true)
            {
                // update the trend every quarter second
                // wait 1.25 seconds before doing anything fire related
                for (int i = 0; i < 4; i++)
                {
                    if (FireOn && FireManAssist.Settings.FiremanManagesBlowerAndDamper && Mode != Mode.Dismissed)
                    {
                        damperIn.ExternalValueUpdate(lastSetDamper);
                        UpdateDamperAndBlower();
                    }
                    foreach (var yieldInstruction in WaitForUnpaused(0.25f)) yield return yieldInstruction;
                }
                if (Firing && FireOn && SufficientReserve && FireManAssist.Settings.FireMode != FireAssistMode.None)
                {
                    FireManAssist.Logger.Log(Time.time + ": Deciding whether to add coal");
                    updateDeltas(Pressure, fireboxTemp.Value, ref t_dot, ref p_dot, ref t_ddot, ref p_ddot, ref lastPressure, ref lastTemperature);
                    FireManAssist.Logger.Log("\tt_dot: " + t_dot + "\tt_ddot: " + t_ddot);
                    FireManAssist.Logger.Log("\tp_dot: " + p_dot + "\tp_ddot: " + p_ddot);
                    FireManAssist.Logger.Log("\tt: " + fireboxTemp.Value + "\tp: " + Pressure);
                    FireManAssist.Logger.Log("\tseconds: " + secondsSinceLastCoal);
                    // It's simple, if pressure isn't rising and we're below target, add coal, make hot.
                    // Don't even try if we're above this pressure
                    var lowPressureThreshold = 1.0f;
                    if (Mode == Mode.Shunt)
                    {
                        lowPressureThreshold = 2.0f;
                    }
                    bool shouldAddCoal = Pressure < (maxPressure - lowPressureThreshold);
                    shouldAddCoal &= determineCoalByTimeAndDeltas(secondsSinceLastCoal, t_dot, t_ddot, p_dot, p_ddot);
                    // extra handle, if we're really low and coal is below 25% full, add more
                    shouldAddCoal = shouldAddCoal && (Mode != Mode.Idle || FireboxContentsNormalized < 0.01f);
                    shouldAddCoal = shouldAddCoal || (Pressure < (maxPressure - 4.0f) && FireboxContentsNormalized < 0.25f);
                    FireManAssist.Logger.Log("Add coal: " + shouldAddCoal);
                    if (shouldAddCoal)
                    {
                        shovelController.AddCoalToFirebox(1);
                        secondsSinceLastCoal = 0;
                    } else
                    {
                        secondsSinceLastCoal++;
                    }
                }
                else if (Firing && SufficientReserve && FireManAssist.Settings.FireMode == FireAssistMode.Full && WaterMonitor.WaterLevel >= 0.75f)
                {
                    FireManAssist.Logger.Log("Igniting fire");
                    // get a fire going because we're supposed to be on, but we're not.
                    shovelController.AddCoalToFirebox(1);
                    secondsSinceLastCoal = 0;
                    fireController.Ignite();
                }
            }
        }

        /// <summary>
        /// Determines whether it's time to add more coal based on how long it's been, and the pressure and temperature trends
        /// </summary>
        /// <param name="secondsSinceLastCoal">Literally how long since we've added coal</param>
        /// <param name="t_dot">The amount temperature has changed in the last second</param>
        /// <param name="t_ddot">The rate of change in temperature change in the last second (this seconds' change - last seconds' change)</param>
        /// <param name="p_dot">The amount pressure has changed in the last second</param>
        /// <param name="p_ddot">The rate of pressure change changing</param>
        /// <returns></returns>
        private bool determineCoalByTimeAndDeltas(int secondsSinceLastCoal, float t_dot, float t_ddot, float p_dot, float p_ddot)
        {
            float min_t_ddot = -0.5f;
            float min_p_ddot = -0.05f;
            int mediumInterval = 2;
            int longInterval = 5;
            if (Mode == Mode.Shunt)
            {
                min_t_ddot = -1.0f;
                min_p_ddot = -0.5f;
                mediumInterval = 5;
                longInterval = 10;
            }
            if (secondsSinceLastCoal > longInterval && (t_dot < min_t_ddot || p_dot < 0))
            {
                return true;
            } else if (secondsSinceLastCoal > mediumInterval && (t_dot < min_t_ddot && p_dot < min_p_ddot))
            {
                return true;
            } else if (t_dot < 0 && t_ddot < 0 && p_dot < 0 && p_ddot < 0)
            {
                return true;
            }
            return false;
        }

        private void updateDeltas(float pressure, float temperature, ref float t_dot, ref float p_dot, ref float t_ddot, ref float p_ddot, ref float lastPressure, ref float lastTemperature)
        {
            var new_t_dot = temperature - lastTemperature;
            t_ddot = new_t_dot - t_dot;
            t_dot = new_t_dot;
            lastTemperature = temperature;
            var new_p_dot = pressure - lastPressure;
            p_ddot = new_p_dot - p_dot;
            p_dot = new_p_dot;
            lastPressure = pressure;
        }

        private IEnumerable WaitForUnpaused(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            yield return new WaitUntil(() =>
                !AppUtil.Instance.IsTimePaused
            );
        }

        private void UpdateDamperAndBlower()
        {
            if (FireManAssist.Settings.FiremanManagesBlowerAndDamper)
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
    }
}
