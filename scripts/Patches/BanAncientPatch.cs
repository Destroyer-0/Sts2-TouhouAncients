using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace TouhouAncients.Scripts.Patches;

/// <summary>
/// 运行时拦截 Ancient 生成，根据 Config 禁用属性从源头过滤。
/// </summary>
[HarmonyPatch(typeof(ActModel), nameof(ActModel.GenerateRooms))]
public static class BanAncientPatch
{
    private static readonly MethodInfo FilterAncientsMethod =
        AccessTools.Method(typeof(BanAncientPatch), nameof(FilterAncients));

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

    private static bool IsBanned(Type type)
    {
        return TouhouAncientsConfig.IsAncientBanned(type) || TouhouAncientsConfig.IsBaseGameAncientBanned(type);
    }
}
