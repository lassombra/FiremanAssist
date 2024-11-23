using DV.Booklets;
using DV.ServicePenalty;
using DV.Simulation.Controllers;
using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace FireManAssist.Patches
{
    //[HarmonyPatch]
    //public static class ResourcePatches
    //{
    //    [HarmonyPatch(typeof(Enum), nameof(Enum.GetValues))]
    //    [HarmonyPostfix]
    //    public static void GetValues(Type enumType, ref Array __result)
    //    {
    //        if (enumType == typeof(ResourceType))
    //        {
    //            var copy = Array.CreateInstance(typeof(int), __result.Length + 1);
    //            Array.Copy(__result, copy, __result.Length);
    //            copy.SetValue(150, __result.Length);
    //            __result = copy;
    //        }
    //    }

    //    [HarmonyPatch(typeof(Enum), nameof(Enum.IsDefined))]
    //    [HarmonyPrefix]
    //    public static bool IsDefined(Type enumType, object value, ref bool __result)
    //    {
    //        if (enumType == typeof(ResourceType))
    //        {
    //            if ((int)value == 150)
    //            {
    //                __result = true;
    //                return false;
    //            }
    //        }
    //        return true;
    //    }

    //    [HarmonyPatch(typeof(TransitionHelpers), nameof(TransitionHelpers.ToV2), new[] {typeof(ResourceType)})]
    //    [HarmonyPrefix]
    //    public static bool ToV2 (ResourceType enumVal, ref ResourceType_v2 __result)
    //    {
    //        if ((int)enumVal == 150)
    //        {
    //            __result = FireManAssist.firemanActiveResource;
    //            return false;
    //        }
    //        return true;
    //    }
    //    [HarmonyPatch(typeof(EnvironmentDamager), nameof(EnvironmentDamager.Init))]
    //    [HarmonyTranspiler]
    //    public static IEnumerable<CodeInstruction> Init(IEnumerable<CodeInstruction> instructions)
    //    {
    //        CodeInstruction instruction;
    //        int found = 0;
    //        var instructionEumerator = instructions.GetEnumerator();
    //        instructionEumerator.MoveNext();
    //        object destination = null;
    //        while (found < 2)
    //        {
    //            instruction = instructionEumerator.Current;
    //            if (instruction.opcode == OpCodes.Beq_S)
    //            {
    //                destination = instruction.operand;
    //                found++;
    //            }
    //            yield return instruction;
    //            instructionEumerator.MoveNext();
    //        }
    //        yield return new CodeInstruction(OpCodes.Ldarg_0);
    //        yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(EnvironmentDamager), "environmentDamageResource"));
    //        yield return new CodeInstruction(OpCodes.Ldc_I4, 150);
    //        yield return new CodeInstruction(OpCodes.Beq_S, destination);
    //        yield return instructionEumerator.Current;
    //        while (instructionEumerator.MoveNext())
    //        {
    //            instruction = instructionEumerator.Current;
    //            yield return instruction;
    //        }
    //    }

    //    [HarmonyPatch(typeof(SimulatedCarDebtTracker), nameof(SimulatedCarDebtTracker.UpdateDebtValues))]
    //    [HarmonyPostfix]
    //    public static void UpdateDebtValuesPostfix(SimulatedCarDebtTracker __instance)
    //    {
    //        var firemanDebt = __instance.GetTrackedDebts().Where(trackedDebt => trackedDebt.Type == (ResourceType)150).FirstOrDefault();
    //        if (firemanDebt != null)
    //        {
    //            var damagers = environmentDamageControllers.GetValue(__instance) as Dictionary<ResourceType, List<EnvironmentDamager>>;
    //            var typeDamagers = damagers[(ResourceType)150];
    //            float damageAccumulated = 0;
    //            foreach (var damager  in typeDamagers)
    //            {
    //                damageAccumulated += damager.Damage;
    //            }
    //            firemanDebt.UpdateStartValue(damageAccumulated);
    //        }
    //    }

    //    [HarmonyPatch(typeof(SimulatedCarDebtTracker), nameof(SimulatedCarDebtTracker.ResetState))]
    //    [HarmonyPostfix]
    //    public static void ResetStatePostfix(SimulatedCarDebtTracker __instance)
    //    {
    //        var firemanDebt = __instance.GetTrackedDebts().Where(trackedDebt => trackedDebt.Type == (ResourceType)150).FirstOrDefault();
    //        if (firemanDebt != null)
    //        {
    //            var damagers = environmentDamageControllers.GetValue(__instance) as Dictionary<ResourceType, List<EnvironmentDamager>>;
    //            var typeDamagers = damagers[(ResourceType)150];
    //            float damageAccumulated = 0;
    //            foreach (var damager in typeDamagers)
    //            {
    //                damager.ResetDamage();
    //            }
    //            firemanDebt.ResetComponent(0f);
    //        }
    //    }

    //    [HarmonyPatch(typeof(BookletCreator_Debt), "GetDebtTitle")]
    //    [HarmonyPrefix]
    //    public static bool DebtTitlePrefix(CarDebtData carDebtData, ResourceType_v2 type, ref string __result)
    //    {
    //        if (type.id == "resource_fireman")
    //        {
    //            __result = "<b>Fireman Labor</b>";
    //            return false;
    //        }
    //        return true;
    //    }

    //    private static TypeInfo simulatedCarType = typeof(SimulatedCarDebtTracker).GetTypeInfo();
    //    private static FieldInfo environmentDamageControllers = simulatedCarType.GetField("resourceToEnvironmentDamage", BindingFlags.Instance | BindingFlags.NonPublic);
    //}
}
