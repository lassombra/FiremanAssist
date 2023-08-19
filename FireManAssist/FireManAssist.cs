using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityModManagerNet;
using static UnityModManagerNet.UnityModManager.ModEntry;

namespace FireManAssist
{
    [EnableReloading]
    public class FireManAssist
    {
        public static ModLogger Logger { get; private set; }
        static bool Load(UnityModManager.ModEntry modEntry)
        {
            modEntry.OnToggle = OnToggle;

            return true;
        }

        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            if (value)
            {
                Logger = modEntry.Logger;
                WorldStreamingInit.LoadingFinished += Start;
                UnloadWatcher.UnloadRequested += Stop;
                if (WorldStreamingInit.IsLoaded)
                {
                    Start();
                }
            } 
            else
            {
                WorldStreamingInit.LoadingFinished -= Start;
                UnloadWatcher.UnloadRequested -= Stop;
                Stop();
            }
            return true;
        }
        static void Start()
        {
            Logger.Log("Starting FireManAssist");
            LocoTracker.Create();
        }
        static void Stop()
        {
            Logger.Log("Stopping FireManAssist");
            LocoTracker.Destroy();
        }
    }
}
