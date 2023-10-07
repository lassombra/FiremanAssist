using CommsRadioAPI;
using DV;
using DV.ThingTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FireManAssist.Radio
{
    internal class RadioToggleBehavior : AStateBehaviour
    {
        TrainCar pointedCar;
        Transform signalOrigin;
        FireMonitor fireMonitor;
        State lastState = State.Off;
        public RadioToggleBehavior() : this(null, null)
        {
        }
        public RadioToggleBehavior(TrainCar pointedCar, FireMonitor monitor): base(new CommsRadioState(titleText: "Fireman Control", contentText: GetContentText(pointedCar, monitor), actionText: GetActionText(pointedCar, monitor)))
        {
            this.pointedCar = pointedCar;
            this.fireMonitor = monitor;
            lastState = monitor?.State ?? State.Off;
            signalOrigin = ((JunctionRemoteLogic)ControllerAPI.GetVanillaMode(VanillaMode.Junction)).signalOrigin;
        }
        public static String GetActionText(TrainCar pointedCar, FireMonitor monitor)
        {
            if (null == pointedCar)
            {
                return "";
            }
            else if (null == monitor)
            {
                return "Call Fireman";
            }
            switch (monitor.State)
            {
                case State.Off:
                case State.ShuttingDown:
                    return "Start Firing";
                case State.Running:
                case State.WaterOut:
                    return "Shut Down";
                default:
                    return "";
            }
        }
        public static String GetContentText(TrainCar pointedCar, FireMonitor monitor)
        {
            if (null == pointedCar)
            {
                return "Select a Steam Locomotive";
            }
            else if (null == monitor)
            {
                return "No Fireman aboard";
            }
            switch (monitor.State)
            {
                case State.Off:
                    return "Fire off";
                case State.ShuttingDown:
                    return "Shutting down fire";
                case State.Running:
                    return "Fire on";
                case State.WaterOut:
                    return "Out of Water";
                default:
                    return "Unknown state";
            }
        }
        public override AStateBehaviour OnUpdate(CommsRadioUtility utility)
        {
            RaycastHit Hit;
            bool found = Physics.Raycast(signalOrigin.position, signalOrigin.forward, out Hit, 100f, LocoTracker.Instance.TrainCarMask);
            bool external = found;
            if (!found)
            {
                found = Physics.Raycast(signalOrigin.position, signalOrigin.forward, out Hit, 100f, LocoTracker.Instance.TrainInteriorMask);
            }
            if (!found&& pointedCar != null)
            {
                HighLighter.Instance.HighlightCar(null);
                return new RadioToggleBehavior(null, null);
            } else if (!found)
            {
                return this;
            }
            TrainCar car = TrainCar.Resolve(Hit.collider.transform);
            switch (car.carType)
            {
                case TrainCarType.LocoSteamHeavy:
                case TrainCarType.LocoS060:
                    return PointAtSteam(car, external);
                default:
                    return PointAtNotSteam(car);
            }
        }
        private AStateBehaviour PointAtSteam(TrainCar car, bool external)
        {
            if (external)
            {
                HighLighter.Instance.HighlightCar(car);
            }
            else
            {
                HighLighter.Instance.HighlightCar(null);
            }
            if (pointedCar == car && null != fireMonitor)
            {
                if (lastState != fireMonitor.State)
                {
                    return new RadioToggleBehavior(pointedCar, fireMonitor);
                }
                return this;
            } 
            FireManAssist.Logger.Log("RadioToggleBehavior.PointAtSteam: pointedCar=" + pointedCar + ", car=" + car);
            FireMonitor monitor = car.GetComponent<FireMonitor>();
            if (pointedCar == car && null == monitor)
            {
                return this;
            }
            return new RadioToggleBehavior(car, monitor);
        }
        private AStateBehaviour PointAtNotSteam(TrainCar car)
        {
            HighLighter.Instance.HighlightCar(null);
            if (pointedCar)
            {
                return new RadioToggleBehavior(null, null);
            } else
            {
                return this;
            }
        }
        public override void OnLeave(CommsRadioUtility utility, AStateBehaviour next)
        {
            HighLighter.Instance.HighlightCar(null);
        }

        public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
        {
            if (action == InputAction.Activate)
            {
                if (pointedCar != null && fireMonitor == null)
                {
                    LocoTracker.Instance.MaybeAttachWaterMonitor(pointedCar);
                } else if (pointedCar != null)
                {
                    fireMonitor.ToggleFiring();
                }
                return new RadioToggleBehavior(pointedCar, pointedCar?.GetComponent<FireMonitor>());
            }
            else
            {
                throw new InvalidOperationException("Only activate is supported");
            }
        }
    }
}
