using CommsRadioAPI;
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
        private FireMonitor fireMonitor;
        private TrainCar trainCar;
        private Mode mode;

        public RadioModeSelectBehaviour(TrainCar trainCar, FireMonitor fireMonitor) : this(trainCar, fireMonitor, fireMonitor.Mode) { }

        public RadioModeSelectBehaviour(TrainCar trainCar, FireMonitor fireMonitor, Mode mode) : base(BuildCommsRadioState(trainCar, fireMonitor, mode))
        {
            this.fireMonitor = fireMonitor;
            this.trainCar = trainCar;
            this.mode = mode;
        }
        private static CommsRadioState BuildCommsRadioState(TrainCar trainCar, FireMonitor fireMonitor, Mode selectedMode)
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
                    fireMonitor.Mode = mode;
                    return new RadioSelectBehaviour(trainCar, fireMonitor);
                case InputAction.Up:
                    var nextIndex = MODE_ORDER.IndexOf(mode) + 1;
                    if (nextIndex >= MODE_ORDER.Count)
                    {
                        nextIndex = 0;
                    }
                    return new RadioModeSelectBehaviour(trainCar, fireMonitor, MODE_ORDER[nextIndex]);
                case InputAction.Down:
                    var prevIndex = MODE_ORDER.IndexOf(mode) - 1;
                    if (prevIndex < 0)
                    {
                        prevIndex = MODE_ORDER.Count - 1;
                    }
                    return new RadioModeSelectBehaviour(trainCar, fireMonitor, MODE_ORDER[prevIndex]);
                default:
                    return new RadioModeSelectBehaviour(trainCar, fireMonitor);
            }
        }
    }
}