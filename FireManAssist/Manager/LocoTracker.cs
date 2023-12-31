﻿using CommsRadioAPI;
using DV.Logic.Job;
using DV.ThingTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FireManAssist
{
    internal class LocoTracker
    {
        private readonly HashSet<TrainCar> monitoredCars = new HashSet<TrainCar>();
        public LayerMask TrainCarMask { get; private set; }
        public LayerMask TrainInteriorMask { get; private set; }
        private void Start()
        {
            FireManAssist.Logger.Log("Starting LocoTracker");
            PlayerManager.CarChanged += PlayerManager_CarChanged;
            this.TrainCarMask = LayerMask.GetMask(new string[]
            {
                "Train_Big_Collider"
            });
            this.TrainInteriorMask = LayerMask.GetMask(new string[]
            {
                "Train_Interior"
            });
            PlayerManager_CarChanged(PlayerManager.Car);
        }
        private void Stop()
        {
            FireManAssist.Logger.Log("Stopping LocoTracker");
            monitoredCars.ToList().ForEach(car =>
            {
                car.TrainsetChanged -= Car_TrainsetChanged;
            });
            PlayerManager.CarChanged -= PlayerManager_CarChanged;
        }
        private void PlayerManager_CarChanged(TrainCar car)
        {
            MaybeAttachWaterMonitorToAllLocos(car);
        }

        private void Car_TrainsetChanged(Trainset trainset)
        {
            trainset?.locoIndices.ForEach(i =>
                {
                    var loco = trainset.cars[i];
                    if (null != loco)
                    {
                        MaybeAttachWaterMonitor(loco);
                    }
                });
        }

        public bool MaybeAttachWaterMonitorToAllLocos(TrainCar car)
        {
            bool attached = false;
            if (null != car)
            {
                attached = MaybeAttachWaterMonitor(car);
            }
            if (attached)
            {
                car.trainset.locoIndices.ForEach(i =>
                {
                    var loco = car.trainset.cars[i];
                    if (null != loco)
                    {
                        MaybeAttachWaterMonitor(loco);
                    }
                });

            }
            return attached;
        }

        public bool MaybeAttachWaterMonitor(TrainCar loco)
        {
            if (null != loco)
            {
                FireManAssist.Logger.Log("MaybeAttachWaterMonitor " + loco.name);
                bool supportedLoco = false;
                switch(loco.carType)
                {
                    case TrainCarType.LocoS060:
                    case TrainCarType.LocoSteamHeavy:
                        supportedLoco = true;
                        break;
                    default:
                        supportedLoco = false;
                        break;
                }
                if (supportedLoco && null == loco.GetComponent<WaterMonitor>())
                {
                    AttachMonitor(loco);
                    return true;
                }
            }
            return false;
        }

        private void AttachMonitor(TrainCar loco)
        {
            FireManAssist.Logger.Log("Attaching WaterMonitor to " + loco.name);
            loco.gameObject.AddComponent<WaterMonitor>();
            loco.gameObject.AddComponent<FireMonitor>();
            loco.OnDestroyCar += Loco_OnDestroyCar;
            monitoredCars.Add(loco);
            loco.TrainsetChanged += Car_TrainsetChanged;
        }

        private void Loco_OnDestroyCar(TrainCar car)
        {
            FireManAssist.Logger.Log("Loco_OnDestroyCar " + car.name);
            car.OnDestroyCar -= Loco_OnDestroyCar;
            car.TrainsetChanged -= Car_TrainsetChanged;
            monitoredCars.Remove(car);
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
}
