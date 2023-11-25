using CommsRadioAPI;
using DV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FireManAssist.Radio
{
    internal class HighLighter
    {
        private static Vector3 HIGHLIGHT_BOUNDS_EXTENSION = new Vector3(0.25f, 0.8f, 0f);
        private static HighLighter instance;
        public static HighLighter Instance => instance ?? (instance = new HighLighter());
        private Material validMaterial;
        private CommsRadioCarDeleter deleteMode;
        private GameObject trainHighlighter;
        private MeshRenderer trainHighlightRender;
        TrainCar lastCar;
        public void HighlightCar(TrainCar car)
        {
            if (null == deleteMode)
            {
                deleteMode = ((CommsRadioCarDeleter)ControllerAPI.GetVanillaMode(VanillaMode.Clear));
            }
            if (null == validMaterial)
            {
                validMaterial = deleteMode.selectionMaterial;
            }
            if (null == trainHighlighter)
            {
                trainHighlighter = deleteMode.trainHighlighter;
            }
            if (null == trainHighlightRender)
            {
                trainHighlightRender = trainHighlighter.GetComponentInChildren<MeshRenderer>(true);
            }

            if (null == car)
            {
                trainHighlighter.SetActive(false);
                lastCar = car;
            }
            else if (lastCar != car)
            {
                trainHighlightRender.material = validMaterial;
                trainHighlighter.transform.localScale = car.Bounds.size + HIGHLIGHT_BOUNDS_EXTENSION;
                Vector3 b = car.transform.up * (trainHighlighter.transform.localScale.y / 2f);
                Vector3 b2 = car.transform.forward * car.Bounds.center.z;
                Vector3 position = car.transform.position + b + b2;
                trainHighlighter.transform.SetPositionAndRotation(position, car.transform.rotation);
                trainHighlighter.SetActive(true);
                trainHighlighter.transform.SetParent(car.transform, true);
                lastCar = car;
            }
        }
        private HighLighter() { 

        }
    }
}
