﻿using CommsRadioAPI;
using DV;
using DV.ThingTypes;
using LocoSim.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FireManAssist.Radio
{
    internal class RadioSelectBehaviour : AStateBehaviour
    {
        TrainCar pointedCar;
        Transform signalOrigin;
        FireMonitor fireMonitor;
        State lastState = State.Off;
        private Boolean reloaded = false;
        public RadioSelectBehaviour() : this(null, null)
        {
        }
        public RadioSelectBehaviour(TrainCar pointedCar, FireMonitor monitor): base(new CommsRadioState(titleText: "Fireman Control", contentText: GetContentText(pointedCar, monitor), actionText: GetActionText(pointedCar, monitor)))
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
            else if (null == monitor || monitor.Mode == Mode.Dismissed)
            {
                return "Call Fireman";
            }
            return "Select mode";
        }
        public static String GetContentText(TrainCar pointedCar, FireMonitor monitor)
        {
            if (null == pointedCar)
            {
                return "Select a Steam Locomotive";
            }
            else if (null == monitor || monitor.Mode == Mode.Dismissed)
            {
                return "No Fireman aboard";
            }
            switch (monitor.State)
            {
                case State.Off:
                    return "Fire off";
                case State.ShuttingDown:
                    return "Shutting down fire";
                case State.WaterOut:
                    return "Out of Water";
                case State.Running:
                    return GetModeText(monitor.Mode);
                default:
                    return "Unknown state";
            }
        }
        public static String GetModeText(Mode mode)
        {
            switch (mode)
            {
                case Mode.Idle:
                    return "Minimum fire";
                case Mode.Shunt:
                    return "Shunting fire";
                case Mode.Road:
                default:
                    return "Full fire";
            }
        }
        public override AStateBehaviour OnUpdate(CommsRadioUtility utility)
        {
            if (reloaded)
            {
                this.reloaded = false;
                return new RadioSelectBehaviour();
            }
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
                return new RadioSelectBehaviour(null, null);
            } else if (!found)
            {
                return this;
            }
            TrainCar car = TrainCar.Resolve(Hit.collider.transform);
            if (null != car.gameObject.GetComponentInChildren<BoilerDefinition>())
            {
                return PointAtSteam(car, external);
            }
            else { 
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
                    return new RadioSelectBehaviour(pointedCar, fireMonitor);
                }
                return this;
            } 
            FireMonitor monitor = car.GetComponent<FireMonitor>();
            if (pointedCar == car && null == monitor)
            {
                return this;
            }
            return new RadioSelectBehaviour(car, monitor);
        }
        private AStateBehaviour PointAtNotSteam(TrainCar car)
        {
            HighLighter.Instance.HighlightCar(null);
            if (pointedCar)
            {
                return new RadioSelectBehaviour(null, null);
            } else
            {
                return this;
            }
        }
        public override void OnEnter(CommsRadioUtility utility, AStateBehaviour previous)
        {
            if (null == previous)
            {
                this.reloaded = true;
            }
            base.OnEnter(utility, previous);
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
                    LocoTracker.Instance.MaybeAttachWaterMonitorToAllLocos(pointedCar, true);
                } else if (pointedCar != null)
                {
                    if (fireMonitor.Mode == Mode.Dismissed)
                    {
                        fireMonitor.Mode = Mode.Off;
                        return new RadioSelectBehaviour(pointedCar, fireMonitor);
                    }
                    else
                    {
                        return new RadioModeSelectBehaviour(pointedCar, fireMonitor);
                    }
                }
                return new RadioSelectBehaviour(pointedCar, pointedCar?.GetComponent<FireMonitor>());
            }
            else
            {
                throw new InvalidOperationException("Only activate is supported");
            }
        }
    }
}
