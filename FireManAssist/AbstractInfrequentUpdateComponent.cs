using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FireManAssist
{
    public abstract class AbstractInfrequentUpdateComponent : MonoBehaviour
    {
        protected int lastUpdate = 0;
        protected int updateInterval = 5;

        protected abstract void Init();
        public void Start()
        {
            Init();
        }
        public virtual void Update()
        {
            lastUpdate++;
            if (lastUpdate >= updateInterval)
            {
                lastUpdate = 0;
                InfrequentUpdate();
            }
        }
        protected abstract void InfrequentUpdate();
    }
}
