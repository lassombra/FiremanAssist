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
        private Port port;
        private Port injector;
        private Port blowdown;

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
            if (!simController.SimulationFlow.TryGetPort("boiler.WATER_LEVEL_NORMALIZED", out this.port))
            {
                Destroy(this);
                FireManAssist.Logger.Log("Water port not found");
                return;
            }
            simController.SimulationFlow.TryGetPort("injector.EXT_IN", out this.injector);
            simController.SimulationFlow.TryGetPort("blowdown.EXT_IN", out this.blowdown);
        }
        public void Update()
        {
            var waterLevel = port.Value;
            if (waterLevel < 0.75f)
            {
                blowdown.ExternalValueUpdate(0f);
                injector.ExternalValueUpdate(1f);
            }
            else if (waterLevel > 0.84f)
            {
                injector.ExternalValueUpdate(0f);
            }
        }
    }
}
