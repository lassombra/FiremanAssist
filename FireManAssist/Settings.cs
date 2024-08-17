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
    public enum FireAssistMode
    {
        None,
        KeepBurning,
        Full
    }
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        [Draw(DrawType.ToggleGroup)] public WaterAssistMode WaterMode = WaterAssistMode.Full;
        [Draw(DrawType.ToggleGroup, Label = "Injector override Mode")] public InjectorOverrideMode InjectorMode = InjectorOverrideMode.Temporary;
        [Draw(DrawType.Toggle, Label ="Enable Auto Cylinder Cocks")] public bool AutoCylinderCocks = false;
        [Draw(DrawType.ToggleGroup, Label= "Fire Assist Mode")] public FireAssistMode FireMode = FireAssistMode.Full;
        [Draw(DrawType.Toggle, Label = "Fireman manages blower and damper")] public bool FiremanManagesBlowerAndDamper = true;
        [Draw(DrawType.Toggle, Label = "Auto Add Fireman")] public bool AutoAddFireman = true;

        public void OnChange()
        { }

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }
    }
}
