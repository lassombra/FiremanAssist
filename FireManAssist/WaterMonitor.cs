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
    internal class WaterMonitor : MonoBehaviour
    {
        private Port waterPort;
        private Port firePort;
        private Port injector;
        private Port blowdown;
        private float injectorTarget = 0.0f;

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
            switch (FireManAssist.Settings.WaterMode)
            {
                case WaterAssistMode.None:
                    return;
                case WaterAssistMode.No_Explosions:
                    MinimumHandler();
                    break;
                case WaterAssistMode.Over_Under_Protection:
                    OverUnderHandler();
                    break;
                case WaterAssistMode.Full:
                    FullHandler();
                    break;
            }
            if (injector.Value != injectorTarget)
            {
                injector.ExternalValueUpdate(injectorTarget);
            }
        }
        private void FullHandler()
        {
            // Determine target water level range to determine injector level
            // then set the injector, and then fall through to OverUnderHandler
            var waterLevel = waterPort.Value;
            // baseWaterLevel is current water level minus the bottom of the glass
            // this creates a range of 0.0 to 0.1 (0.75 to 0.85)
            var baseWaterLevel = waterLevel - 0.75f;
            // target injector level is 1.0 minus the baseWaterLevel - that is when the base level is 0, it'll be 1.0, as the water level rises, injector level will fall.
            // this is capped to 1.0f and floored to 0.0f to prevent the injector from being set to a negative value, or beyond 100%
            // then it's converted to a float with 1 decimal place to reduce injector thrashing.
            float injectorLevel = (float)Math.Round(Math.Max(0.0f, Math.Min(1.0f, 1f - (baseWaterLevel * 10f))),1);
            injectorTarget = injectorLevel;
            // this fall through ensures that the protection rules kick in, even if the injector is set to 100%
            OverUnderHandler(waterLevel);
        }
        private void OverUnderHandler(float waterLevel = 0.0f)
        {
            if (waterLevel == 0.0f)
            {
                waterLevel = waterPort.Value;
            }
            // If water level is above 0.84, turn off injector.
            // otherwise fall through to MinimumHandler
            if (waterLevel > 0.84f)
            {
                injectorTarget = 0.0f;
            }
            else
            {
                MinimumHandler(waterLevel);
            }
        }
        private void MinimumHandler(float waterLevel = 0.0f)
        {
            // If water level is below 0.75, turn off blowdown.
            // if fire is on also, turn on injector
            if (waterLevel == 0.0f)
            {
                waterLevel = waterPort.Value;
            }
            if (waterLevel < 0.75f)
            {
                blowdown.ExternalValueUpdate(0f);
                if (firePort.Value == 1f)
                {
                    injectorTarget = 1.0f;
                }
            }
        }
    }
}
