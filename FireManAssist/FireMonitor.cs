using DV.Simulation.Cars;
using DV.Simulation.Controllers;
using LocoSim.Implementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FireManAssist
{
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

        // this is the firebox itself - it's a useful object as it has a lot of the variables we need
        // and it's guaranteed to be present for the two locomotives we're supporting
        private FireboxSimController fireController;
        // This is how keyboard coal add is simulated.  We use it to add coal to the firebox.
        private MagicShoveling shovelController;

        private Port reverser;
        private Port throttle;

        private Single lastSetDamper;
        private readonly PressureTracker pressureTracker = new PressureTracker();
        private Single lastTarget = 0.0f;

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
            fireController = trainCar.GetComponent<SimController>().firebox;
            shovelController = trainCar.GetComponent<MagicShoveling>();
            simController.SimulationFlow.TryGetPort("damper.EXT_IN", out this.damperIn);
            simController.SimulationFlow.TryGetPort("blower.EXT_IN", out this.blowerIn);
            simController.SimulationFlow.TryGetPort("boiler.PRESSURE", out this.boilerPressure);
            simController.SimulationFlow.TryGetPort("exhaust.AIR_FLOW", out this.airflow);
            simController.SimulationFlow.TryGetPort("reverser.REVERSER", out this.reverser);
            simController.SimulationFlow.TryGetPort("throttle.EXT_IN", out this.throttle);
            //Offset from water monitor since these get added in the same tick
            lastUpdate = 3;
        }
        public Single AirFlow
        {
            get
            {
                return airflow.Value;
            }
        }
        public Single Pressure
        {
            get
            {
                return boilerPressure.Value;
            }
        }
        public Boolean FireDoorOpen
        {
            get
            {
                return fireController.FireboxDoorOpening > 0.0f;
            }
        }
        public Boolean FireOn
        {
            get
            {
                return fireController.IsFireOn;
            }
        }
        public Single FireboxContentsNormalized
        {
            get
            {
                return fireController.FireboxContents / fireController.FireboxCapacity;
            }
        }
        private static Single FireboxTarget(Trend trend, Single pressure)
        {
            var target = 0.0f;
            switch (trend)
            {
                // minimum coal - it's rising, don't do much
                case Trend.Rising:
                    target = 0.05f;
                    break;
                // it's falling - we might need to add a lot of coal, but we'll plan it based on the range of 14.5 to 13.5 (actually 13.5 to 12.5)
                case Trend.Falling:
                    target = FireManAssist.CalculateIntervalFromCurve(pressure, 2.0f, 13.5f, 14.5f, 0.01f);
                    break;
                default:
                // Use a cube root curve based on current pressure - this will ramp down rapidly.
                    target = FireManAssist.CalculateIntervalFromCurve(pressure, 3.0f, 13.5f, 14.5f, 0.01f);
                    break;
            }
            // Never more than 85% full, and never less than 0% full.
            return Math.Min(Math.Max(target, 0.0f), 0.85f);
        }
        private Single normalize(Single value, Single min, Single max)
        {
            return Math.Max(0.0f, Math.Min(1.0f, (value - min) / (max - min)));
        }
        protected override void InfrequentUpdate(bool slowUpdateFrame)
        {
            var trend = pressureTracker.UpdateAndCheckTrend(Pressure);
            if (FireDoorOpen && FireOn)
            {
                var target = FireboxTarget(trend, Pressure) * fireController.FireboxDoorOpening;
                // if we aren't demanding much from the locomotive, we don't need to add much coal.
                target = target * normalize(Math.Abs(throttle.Value), 0.01f, 0.85f) * normalize(Math.Abs(reverser.Value), 0.01f, 0.75f);
                if (Pressure < 11.0f)
                {
                    // during startup / if we've run out of pressure, we have a *minimum* of 25% coal
                    target = Math.Max(target, 0.25f);
                }
                // anytime we're trying to manage the fire, we need a minimum of 1% coal
                target = Math.Max(target, 0.01f);
                lastTarget = target;
                Boolean shouldAddCoal = FireboxContentsNormalized <= target;
                // need some pressure
                // shouldAddCoal = shouldAddCoal || (trend == Trend.Falling && Pressure <= 13.5f && FireboxContentsNormalized <= 0.5f);
                // can't keep up with demand
                // shouldAddCoal = shouldAddCoal || (trend != Trend.Rising && Pressure <= 13.0f && FireboxContentsNormalized <= 0.9f);
                if (shouldAddCoal)
                {
                    shovelController.AddCoalToFirebox(1);
                }
                //keeps fire from getting too hot
                if (slowUpdateFrame)
                {
                    lastSetDamper = FireManAssist.CalculateIntervalFromCurve(Pressure, 2.0f, 13.5f, 14.5f, 0.2f);
                }
                if (Pressure <= 13.5f && AirFlow <= 1.5f)
                {
                    blowerIn.ExternalValueUpdate(1.0f);
                } else if (Pressure >= 13f && AirFlow >= 2.0f)
                {
                    blowerIn.ExternalValueUpdate(0.0f);
                }
            }
        }
        public override void Update()
        {
            base.Update();
            if (FireDoorOpen && FireOn)
            {
                damperIn.ExternalValueUpdate(lastSetDamper);
            }
        }
    }
}
