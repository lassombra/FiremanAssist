using CommsRadioAPI;
using FireManAssist.Manager;
using System.Collections.Generic;
using UnityEngine;

namespace FireManAssist.Radio
{
    internal class RadioModeSelectBehaviour : AStateBehaviour
    {
        private static readonly List<Mode> MODE_ORDER = new List<Mode>(new Mode[]
        {
            Mode.Off,
            Mode.Idle,
            Mode.Shunt,
            Mode.Road,
            Mode.Dismissed
        });
        private FireModeController fireModeController;
        private TrainCar trainCar;
        private Mode mode;

        public RadioModeSelectBehaviour(TrainCar trainCar, FireModeController fireModeController) : this(trainCar, fireModeController, fireModeController.Mode) { }

        public RadioModeSelectBehaviour(TrainCar trainCar, FireModeController fireModeController, Mode mode) : base(BuildCommsRadioState(trainCar, fireModeController, mode))
        {
            this.fireModeController = fireModeController;
            this.trainCar = trainCar;
            this.mode = mode;
        }
        private static CommsRadioState BuildCommsRadioState(TrainCar trainCar, FireModeController fireMonitor, Mode selectedMode)
        {
            var actionText = "";
            switch (selectedMode)
            {
                case Mode.Dismissed:
                    actionText = "Dismiss";
                    break;
                case Mode.Idle:
                    actionText = "Minimum Fire";
                    break;
                case Mode.Shunt:
                    actionText = "Shunting Fire";
                    break;
                case Mode.Road:
                    actionText = "Full Fire";
                    break;
                case Mode.Off:
                    actionText = "Shutdown";
                    break;
            }
            return new CommsRadioState(
                titleText: "Fireman Control",
                contentText: RadioSelectBehaviour.GetContentText(trainCar, fireMonitor),
                actionText: selectedMode == fireMonitor.Mode ? "Cancel" : actionText,
                buttonBehaviour: DV.ButtonBehaviourType.Override
                );
        }

        public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
        {
            switch (action)
            {
                case InputAction.Activate:
                    fireModeController.Mode = mode;
                    return new RadioSelectBehaviour(trainCar, fireModeController);
                case InputAction.Down:
                    var nextIndex = MODE_ORDER.IndexOf(mode) + 1;
                    if (nextIndex >= MODE_ORDER.Count)
                    {
                        nextIndex = 0;
                    }
                    return new RadioModeSelectBehaviour(trainCar, fireModeController, MODE_ORDER[nextIndex]);
                case InputAction.Up:
                    var prevIndex = MODE_ORDER.IndexOf(mode) - 1;
                    if (prevIndex < 0)
                    {
                        prevIndex = MODE_ORDER.Count - 1;
                    }
                    return new RadioModeSelectBehaviour(trainCar, fireModeController, MODE_ORDER[prevIndex]);
                default:
                    return new RadioModeSelectBehaviour(trainCar, fireModeController);
            }
        }
    }
}