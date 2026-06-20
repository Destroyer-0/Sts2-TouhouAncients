using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BaseLib.Abstracts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace TouhouAncients.Scripts.Patches;

/// <summary>
/// 运行时拦截所有 Ancient 的生成和合法性检查，根据 Config 布尔属性决定是否禁用。
/// </summary>
public static class BanAncientPatch
{

    /// <summary>
    /// Transpiler 拦截 ActModel.GenerateRooms：
    /// 在 GetUnlockedAncients().Concat(shared) 后注入 FilterAncients 过滤，
    /// 从源头剔除被禁用的 Ancient，避免事后替换。
    /// </summary>
    [HarmonyPatch(typeof(ActModel), nameof(ActModel.GenerateRooms))]
    public static class ActModel_GenerateRooms_Transpiler
    {
        private static readonly MethodInfo FilterAncientsMethod =
            AccessTools.Method(
                typeof(ActModel_GenerateRooms_Transpiler),
                nameof(FilterAncients));

        private static readonly MethodInfo EnumerableConcatMethod =
            AccessTools.Method(typeof(Enumerable), nameof(Enumerable.Concat))
                .MakeGenericMethod(typeof(AncientEventModel));

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                yield return instruction;

                // Emit after:
                //   GetUnlockedAncients(unlockState)
                //       .Concat(_sharedAncientSubset ?? new List<AncientEventModel>())
                //
                // inject:
                //   .FilterAncients()
                if (instruction.Calls(EnumerableConcatMethod))
                {
                    yield return new CodeInstruction(OpCodes.Call, FilterAncientsMethod);
                }
            }
        }

        private static IEnumerable<AncientEventModel> FilterAncients(IEnumerable<AncientEventModel> ancients)
        {
            return ancients.Where(a => !IsBanned(a.GetType())).ToArray();
        }
    }

    /// <summary>
    /// Prefix 拦截 CustomAncientModel.IsValidForAct(ActModel)
    /// </summary>
    [HarmonyPatch]
    public static class CustomAncientModel_IsValidForAct_Patch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(CustomAncientModel), nameof(CustomAncientModel.IsValidForAct));
        }

        [HarmonyPrefix]
        private static bool Prefix(CustomAncientModel __instance, ref bool __result)
        {
            if (IsBanned(__instance.GetType()))
            {
                __result = false;
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Prefix 拦截 AbstractModel.ShouldAllowAncient(Player, AncientEventModel)
    /// </summary>
    [HarmonyPatch]
    public static class AbstractModel_ShouldAllowAncient_Patch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(AbstractModel), nameof(AbstractModel.ShouldAllowAncient));
        }

        [HarmonyPrefix]
        private static bool Prefix(AbstractModel __instance, AncientEventModel ancient, ref bool __result)
        {
            if (IsBanned(ancient.GetType()))
            {
                __result = false;
                return false;
            }
            return true;
        }
    }

    private static bool IsBanned(Type type)
    {
        return TouhouAncientsConfig.IsAncientBanned(type) || TouhouAncientsConfig.IsBaseGameAncientBanned(type);
    }
}
