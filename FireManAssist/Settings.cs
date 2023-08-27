using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityModManagerNet;

namespace FireManAssist
{
    public enum WaterAssistMode
    {
        None,
        No_Explosions,
        Over_Under_Protection,
        Full
    }
    public enum InjectorOverrideMode
    {
        None,
        Temporary,
        Complete
    }
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        [Draw(DrawType.ToggleGroup)] public WaterAssistMode WaterMode = WaterAssistMode.Full;
        [Draw(DrawType.ToggleGroup, Label = "Injector override Mode")] public InjectorOverrideMode InjectorMode = InjectorOverrideMode.Temporary;

        public void OnChange()
        { }

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }
    }
}
