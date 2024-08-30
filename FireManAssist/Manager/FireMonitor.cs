using DV;
using DV.JObjectExtstensions;
using DV.Simulation.Cars;
using DV.Simulation.Controllers;
using FireManAssist.Manager;
using LocoSim.Definitions;
using LocoSim.Implementations;
using Newtonsoft.Json.Linq;
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
        Dismissed,
        Off,
        Idle,
        Shunt,
        Road
    }
    public class FireMonitor : SimComponent
    {
        // Control for damper
        private PortReference damperIn;
        // control for blower
        private PortReference blowerIn;
        // current airflow through the firebox - this is used instead of forward speed because it takes into account more variables
        // a real fireman would have a pretty good idea of what this would be in a given situation, so it's a pretty good proxy for "locomotive state"
        // when low and pressure is low, then the blower should be on
        private PortReference airflow;

        // The boiler pressure port, used to monitor the boiler pressure and adjust the damper curves accordingly
        private PortReference boilerPressure;
        private PortReference boilerWaterLevel;

        // This is how keyboard coal add is simulated.  We use it to add coal to the firebox.
        private MagicShoveling shovelController;

        private Single lastSetDamper;
        private PortReference waterNormalized;
        private PortReference fireboxTemp;
        private readonly float minReserve = 0.1f;
        private FireMonitorDefinition definition;
        private PortReference ignition;
        private PortReference firePort;
        private PortReference coalLevel;
        private PortReference coalCapacity;
        private Port firing;
        private Port state;
        private Port mode;

        public FireMonitor(FireMonitorDefinition definition) : base(definition.ID)
        {
            damperIn = base.AddPortReference(definition.damperIn);
            blowerIn = base.AddPortReference(definition.blowerIn);
            airflow = base.AddPortReference(definition.airflow);
            boilerPressure = base.AddPortReference(definition.boilerPressure);
            boilerWaterLevel = base.AddPortReference(definition.boilerWaterLevel);
            waterNormalized = base.AddPortReference(definition.waterNormalized);
            fireboxTemp = base.AddPortReference(definition.fireboxTemp);
            ignition = base.AddPortReference(definition.ignition);
            firePort = base.AddPortReference(definition.firePort);
            coalLevel = base.AddPortReference(definition.coalLevel);
            coalCapacity = base.AddPortReference(definition.coalCapacity);
            firing = base.AddPort(definition.firing);
            state = base.AddPort(definition.condition);
            mode = base.AddPort(definition.mode, (float)Mode.Dismissed);
            shovelController = definition.shoveling;
            this.definition = definition;
        }

        private State CalculateState()
        {
            if (Firing && SufficientReserve)
            {
                return State.Running;
            }
            else if (Firing)
            {
                return State.WaterOut;
            }
            else if (FireOn)
            {
                return State.ShuttingDown;
            }
            else
            {
                return State.Off;
            }
        }
        public State State
        {
            get
            {
                return (State)state.Value;
            }
        }
        public Mode Mode
        {
            get
            {
                return (Mode)mode.Value;
            }
            set
            {
                mode.Value = (float)value;
            }
        }
        public bool Firing { get
            {
                var selectedMode = (Mode)mode.Value;
                return selectedMode != Mode.Off && selectedMode != Mode.Dismissed;
            }
        }

        public Single AirFlow => airflow.Value;
        public Single Pressure => boilerPressure.Value;
        public Boolean FireOn => firePort.Value > 0;
        public Single FireboxContentsNormalized => coalLevel.Value / coalCapacity.Value;
        public Single WaterReserve => this.waterNormalized.Value;
        public bool SufficientReserve => this.waterNormalized.Value >= this.minReserve;
        public Single MaxPressure => definition.boiler.safetyValveOpeningPressure;
        public Single PassiveExhaust => definition.steamExhaust.passiveExhaust;
        public Single MaxBlowerFlow => definition.steamExhaust.maxBlowerFlow;

        private Single Normalize(Single value, Single min, Single max)
        {
            return Math.Max(0.0f, Math.Min(1.0f, (value - min) / (max - min)));
        }
        private float Waiting { get; set; } = 0.0f;
        private IEnumerator<float> _Sequence;
        private IEnumerator<float> Sequence { get
            {
                if (_Sequence == null)
                {
                    _Sequence = FiremanUpdate();
                }
                return _Sequence;
            } }
        protected IEnumerator<float> FiremanUpdate()
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
                    if (FireOn && FireManAssist.Settings.FiremanManagesBlowerAndDamper && (Mode)mode.Value != Mode.Dismissed)
                    {
                        damperIn.Value = lastSetDamper;
                        UpdateDamperAndBlower();
                    }
                    yield return 0.25f;
                }
                if (Firing && FireOn && SufficientReserve && FireManAssist.Settings.FireMode != FireAssistMode.None)
                {
                    updateDeltas(Pressure, fireboxTemp.Value, ref t_dot, ref p_dot, ref t_ddot, ref p_ddot, ref lastPressure, ref lastTemperature);
                    // It's simple, if pressure isn't rising and we're below target, add coal, make hot.
                    // Don't even try if we're above this pressure
                    var lowPressureThreshold = 1.0f;
                    if ((Mode)mode.Value == Mode.Shunt)
                    {
                        lowPressureThreshold = 2.0f;
                    }
                    bool shouldAddCoal = Pressure < (MaxPressure - lowPressureThreshold);
                    shouldAddCoal &= determineCoalByTimeAndDeltas(secondsSinceLastCoal, t_dot, t_ddot, p_dot, p_ddot);
                    // extra handle, if we're really low and coal is below 25% full, add more
                    shouldAddCoal = shouldAddCoal && ((Mode)mode.Value != Mode.Idle || FireboxContentsNormalized < 0.01f);
                    shouldAddCoal = shouldAddCoal || (Pressure < (MaxPressure - 4.0f) && FireboxContentsNormalized < 0.15f);
                    if (shouldAddCoal)
                    {
                        shovelController.AddCoalToFirebox(1);
                        secondsSinceLastCoal = 0;
                    } else
                    {
                        secondsSinceLastCoal++;
                    }
                }
                else if (Firing && SufficientReserve && FireManAssist.Settings.FireMode == FireAssistMode.Full && boilerWaterLevel.Value >= 0.75f)
                {
                    FireManAssist.Logger.Log("Igniting fire");
                    // get a fire going because we're supposed to be on, but we're not.
                    shovelController.AddCoalToFirebox(1);
                    secondsSinceLastCoal = 0;
                    ignition.Value = 1f;
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
            if ((Mode)mode.Value == Mode.Shunt)
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

        private void UpdateDamperAndBlower()
        {
            if (FireManAssist.Settings.FiremanManagesBlowerAndDamper)
            {
                // Up to maxPressure - 1 bar we're still trying to add pressure, so don't limit the fire temp.
                // As we cross that, we want to chill the fire by closing the damper all the way until we are 0.5 bar below safety
                // at which point we want damper full.
                // We prefer to max out damper at this point as coal lasts longer than water, and it's better to waste a bit of coal burning poorly than to
                // waste water popping the safety.
                lastSetDamper = FireManAssist.CalculateIntervalFromCurve(Pressure, 0.5f, MaxPressure - 1.0f, MaxPressure - 0.5f, 0.2f);
                // If we're still in startup mode, or airflow is abysmal (because we're moving very slowly) then turn on the blower - this is especially useful
                // to get pressure built back up after a hill if the driver closes the throttle which reduces airflow
                if ((Pressure <= (MaxPressure - 3.0f) && AirFlow <= (PassiveExhaust + MaxBlowerFlow)) && lastSetDamper >= 0.99f)
                {
                    blowerIn.Value = 1.0f;
                }
                // if pressure is high and airflow is still moderate or if airflow has started in earnest, or the damper is 
                // being closed, then we definitely don't want the blower.
                else if (Pressure >= (MaxPressure - 1.5f) || AirFlow > (PassiveExhaust + (2 * MaxBlowerFlow)) || lastSetDamper <0.99f)
                {
                    blowerIn.Value = 0.0f;
                }

            }
        }

        public override void Tick(float delta)
        {
            if (this.firing.Value == 0 && (this.FireOn || (this.Mode != Mode.Dismissed && this.Mode != Mode.Off))) {
                this.firing.Value = 1;
            } else if (this.firing.Value == 1 && (!this.FireOn && (this.Mode == Mode.Dismissed || this.Mode == Mode.Off))) {
                this.firing.Value = 0;
            }
            UpdateState();
            if (Waiting > 0.0f)
            {
                Waiting -= delta;
                return;
            }
            Sequence.MoveNext();
            Waiting = Sequence.Current;
        }

        private void UpdateState()
        {
            var state = CalculateState();
            if (this.State != state)
            {
                this.state.Value = (float)state;
            }
        }
        public override bool HasSaveData { get { return true; } }
        public override JObject GetSaveStateData()
        {
            var saveData = new JObject();
            saveData.SetInt("mode", (int)(this.mode.Value));
            return saveData;
        }
        public override void SetSaveStateData(JObject savedData)
        {
            this.mode.Value = (float)savedData.GetInt("mode");
        }
    }
}
