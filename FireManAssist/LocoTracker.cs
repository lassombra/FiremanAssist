using DV.ThingTypes;
using RootMotion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FireManAssist
{
    internal class LocoTracker
    {
        private void Start()
        {
            FireManAssist.Logger.Log("Starting LocoTracker");
            PlayerManager.CarChanged += PlayerManager_CarChanged;
        }
        private void Stop()
        {
            FireManAssist.Logger.Log("Stopping LocoTracker");
            PlayerManager.CarChanged -= PlayerManager_CarChanged;
        }

        private void PlayerManager_CarChanged(TrainCar car)
        {
            if (null != car)
            {
                switch (car.carType)
                {
                    case TrainCarType.LocoSteamHeavy:
                    case TrainCarType.LocoS060:
                        MaybeAttachWaterMonitor(car);
                        break;
                    default:
                        break;
                }
                
            }
        }
        private void MaybeAttachWaterMonitor(TrainCar loco)
        {
            if (null != loco)
            {
                var waterMonitor = loco.GetComponent<WaterMonitor>();
                if (null == waterMonitor)
                {
                    FireManAssist.Logger.Log("Attaching WaterMonitor to " + loco.name);
                    loco.gameObject.AddComponent<WaterMonitor>();
                }
            }
        }
        private static LocoTracker instance;
        public static void Create()
        {
            if (null == instance)
            {
                instance = new LocoTracker();
                instance.Start();
            }
        }
        public static void Destroy()
        {
            if (null != instance)
            {
                instance.Stop();
                instance = null;
            }
        }
        public static LocoTracker Instance
        {
            get
            {
                return instance;
            }
        }
    }
    // S282 - 75% to 85%

}
