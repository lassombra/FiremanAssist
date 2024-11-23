using DV.Simulation.Controllers;
using FireManAssist.Patches;
using LocoSim.Implementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FireManAssist.Manager
{
    public class FireModeController : ASimInitializedController
    {
        private Port mode;
        private Port condition;
        private Port firing;

        public Mode Mode
        {
            get
            {
                return (Mode)mode.Value;
            }
            set
            {
                mode.ExternalValueUpdate((float)value);
            }
        }

        public State Condition
        {
            get
            {
                return (State)condition.Value;
            }
        }

        public bool Firing
        {
            get
            {
                return firing.Value > 0;
            }
        }

        public override void Init(TrainCar car, SimulationFlow simFlow)
        {
            var firemanControllerDefinition = car.GetComponentInChildren<FireMonitorDefinition>();
            if (firemanControllerDefinition != null )
            {
                simFlow.TryGetPort(PortHelpers.MakePortId(firemanControllerDefinition, firemanControllerDefinition.mode), out this.mode);
                simFlow.TryGetPort(PortHelpers.MakePortId(firemanControllerDefinition, firemanControllerDefinition.condition), out this.condition);
                simFlow.TryGetPort(PortHelpers.MakePortId(firemanControllerDefinition, firemanControllerDefinition.firing), out this.firing);
            }
        }
    }
}
