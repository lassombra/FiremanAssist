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
        protected int slowUpdateInterval = 30;


        protected abstract void Init();
        public void Start()
        {
            Init();
        }
        public virtual void Update()
        {
            lastUpdate++;
            if (lastUpdate >= slowUpdateInterval)
            {
                lastUpdate = 0;
                InfrequentUpdate(true);
            } else if (lastUpdate % updateInterval == 0){
                InfrequentUpdate(false);
            }
        }

        /// <summary>
        /// Called infrequently (by default every 5 ticks) to perform updates
        /// </summary>
        /// <param name="slowUpdateFrame">
        /// Whether or not this is the extra slow frame - note that the idea is that some updates will happen every frame, some every (5 by default) ticks, 
        /// some every (30 by default) ticks.  Namely actually recalculation of target positions should be delayed to avoid "twitching" controls.
        /// </param>
        protected abstract void InfrequentUpdate(bool slowUpdateFrame);
    }
}
