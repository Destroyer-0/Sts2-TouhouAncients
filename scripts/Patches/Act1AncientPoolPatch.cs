using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Unlocks;

namespace TouhouAncients.Scripts.Patches;

/// <summary>
/// 让第一幕（Act 1）也能出现本 Mod 的先古之民。
///
/// 背景：原版 <c>ActModel.GenerateRooms</c> 的候选是
/// <c>GetUnlockedAncients(unlockState).Concat(_sharedAncientSubset ?? [])</c>，而 <c>_sharedAncientSubset</c>
/// 只对第二幕及之后有值（原版 <c>RunManager.GenerateRooms</c> 里是 <c>State.Acts.Skip(1)</c> 才调
/// <c>SetSharedAncientSubset</c>），第一幕的 <c>Overgrowth</c> / <c>Underdocks</c> 的 <c>AllAncients</c>
/// 又硬编码为单个涅奥，所以 BaseLib 的 <c>AddCustomAncientsToPool</c> 与 <c>IsValidForAct</c>
/// 都够不到第一幕。
///
/// 本补丁直接给第一幕的 <c>GetUnlockedAncients</c> 追加候选——与 RitsuLib 的
/// <c>DynamicActContentPatcher</c> 注入按幕先古之民是同一套机制。原版的 <c>rng.NextItem</c> 会自动
/// 把它们与涅奥同池均匀抽取（各占 1/(N+1)），因此不需要重抽，也不影响原版随机流。
///
/// 只追加本 Mod 自己的 <see cref="TouhouAncientBase.ShowAct"/> == 1 的先古之民：其他 Mod 的一层
/// 先古之民由它们自己用同样的方式注入（BaseLib 没有一层注入通道，只声明 <c>IsValidForAct</c>
/// 不会让它们出现在第一幕）。追加前按 Id 去重，别的补丁已注入过的同一个先古之民不会被算两次。
/// </summary>
[HarmonyPatch]
internal static class Act1AncientPoolPatch
{
    /// <summary>
    /// 要 patch 的目标：各幕声明实现的 <c>GetUnlockedAncients(UnlockState)</c>。
    /// 该方法在 <see cref="ActModel"/> 上是 abstract，具体实现分散在各幕（含 Mod 幕继承的基类实现），
    /// 所以沿 BaseType 向上找「声明且非抽象」的那一个，并按方法去重。
    /// </summary>
    private static IEnumerable<MethodBase> TargetMethods()
    {
        HashSet<MethodBase> seen = new HashSet<MethodBase>();

        foreach (Type type in ModelDb.AllAbstractModelSubtypes)
        {
            if (type == null || type.IsAbstract || type.IsInterface || !typeof(ActModel).IsAssignableFrom(type))
            {
                continue;
            }

            MethodInfo? method = FindDeclaredGetUnlockedAncients(type);
            if (method != null && seen.Add(method))
            {
                yield return method;
            }
        }
    }

    /// <summary>
    /// 沿 BaseType 向上找声明了 <c>GetUnlockedAncients(UnlockState)</c> 的非抽象方法，找不到返回 null。
    /// </summary>
    private static MethodInfo? FindDeclaredGetUnlockedAncients(Type actType)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        Type[] parameters = [typeof(UnlockState)];

        for (Type? type = actType; type != null && typeof(ActModel).IsAssignableFrom(type); type = type.BaseType)
        {
            if (type.GetMethod(nameof(ActModel.GetUnlockedAncients), flags, null, parameters, null) is { IsAbstract: false } method)
            {
                return method;
            }
        }

        return null;
    }

    /// <summary>
    /// 把本 Mod 的一层先古之民追加进候选列表。只处理第一幕（原版 Overgrowth / Underdocks 的
    /// <c>Index</c> 都是 0），二/三幕与共享池完全不动。
    /// </summary>
    [HarmonyPostfix]
    private static void AddAct1Ancients(ActModel __instance, ref IEnumerable<AncientEventModel> __result)
    {
        if (__instance.Index != 0 || __result == null)
        {
            return;
        }

        List<AncientEventModel> existing = __result.ToList();
        List<AncientEventModel> candidates = CollectAct1Ancients(existing);
        if (candidates.Count == 0)
        {
            return;
        }

        // 追加在末尾：原版 NextItem 按下标抽取，候选顺序只取决于排序结果，各端一致。
        __result = existing.Concat(candidates);
    }

    /// <summary>
    /// 收集要补进第一幕的本 Mod 先古之民：<see cref="TouhouAncientBase.ShowAct"/> == 1、未被禁用、
    /// 且不在 <paramref name="existing"/> 里。结果按 Id.Entry 排序，保证各端候选顺序一致。
    /// </summary>
    private static List<AncientEventModel> CollectAct1Ancients(List<AncientEventModel> existing)
    {
        HashSet<ModelId> present = existing.Select(ancient => ancient.Id).ToHashSet();
        List<AncientEventModel> result = new List<AncientEventModel>();

        foreach (Type type in ModelDb.AllAbstractModelSubtypes)
        {
            if (type == null || type.IsAbstract || type.IsInterface || !typeof(TouhouAncientBase).IsAssignableFrom(type))
            {
                continue;
            }

            if (ModelDb.GetByIdOrNull<AncientEventModel>(ModelDb.GetId(type)) is not TouhouAncientBase ancient)
            {
                continue;
            }

            if (ancient.ShowAct != 1)
            {
                continue;
            }

            if (TouhouAncientsConfig.IsAncientBanned(type))
            {
                continue;
            }

            // 别的补丁（或其他库）已经注入过的同一个先古之民不重复追加。
            if (!present.Add(ancient.Id))
            {
                continue;
            }

            result.Add(ancient);
        }

        result.Sort((left, right) => string.Compare(left.Id.Entry, right.Id.Entry, StringComparison.Ordinal));
        return result;
    }
}
