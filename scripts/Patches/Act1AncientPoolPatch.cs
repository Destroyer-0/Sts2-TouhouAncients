using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseLib.Patches.Content;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;

namespace TouhouAncients.Scripts.Patches;

/// <summary>
/// 让第一幕（Act 1）也能出现本 Mod 的先古之民。
///
/// 背景：BaseLib 的 <c>AddCustomAncientsToPool</c> 在 <c>ActModel.GenerateRooms</c> 的 Prefix 里
/// 把自定义先古之民塞进 <c>_sharedAncientSubset</c>，但该字段只对第二幕及之后有值
/// （原版 <c>RunManager.GenerateRooms</c> 里是 <c>State.Acts.Skip(1)</c> 才调
/// <c>SetSharedAncientSubset</c>），因此第一幕的自定义先古之民永远不会被注入。
/// 同理第一幕的 <c>AllAncients</c> 是硬编码的单元素列表（原版涅奥），也没有追加候选的通道。
///
/// 本补丁用 Postfix 接管第一幕：在 <c>ActModel.GenerateRooms</c> 已经抽完 <c>_rooms.Ancient</c>
/// 之后，把「本 Mod 中 <see cref="TouhouAncientBase.ShowAct"/> 为 1 且通过合法性校验的先古之民」
/// 与原版抽中的先古之民（第一幕是涅奥）合并成一个候选列表重新均匀抽取，
/// 使每个候选的出现概率相同（涅奥与每个自定义先古之民各占 1/(N+1)）。
///
/// 禁用：配置「初始先古之民配置」里的「禁用涅奥」生效于本补丁——涅奥被禁用时不再进入
/// 上述候选列表，第一幕只从本 Mod 的一层先古之民中抽取；本 Mod 的一层先古之民被禁用时
/// （<see cref="TouhouAncientsConfig.IsAncientBanned(Type)"/>）由 <see cref="CollectAct1Candidates"/>
/// 提前剔除。若涅奥与所有一层先古之民都被禁用，则兜底保留原版抽签结果。
///
/// 只对第一幕生效：
///   - <c>____sharedAncientSubset != null</c> 说明是第二幕及之后，直接跳过，完全交给 BaseLib；
///   - <c>Index != 0</c> 再兜一层（原版 Overgrowth / Underdocks 的 Index 都是 0）。
/// </summary>
[HarmonyPatch(typeof(ActModel), nameof(ActModel.GenerateRooms))]
internal static class Act1AncientPoolPatch
{
    /// <summary>原版私有字段 <c>ActModel._rooms</c>；<c>Ancient</c> 只有 getter 无 setter 通道时经此写入。</summary>
    private static readonly FieldInfo RoomsField = AccessTools.Field(typeof(ActModel), "_rooms");

    /// <summary>
    /// 第一幕补抽。<paramref name="____sharedAncientSubset"/> 由 Harmony 按私有字段
    /// <c>ActModel._sharedAncientSubset</c> 注入（命名规则与 BaseLib 的同名补丁一致）。
    /// </summary>
    [HarmonyPostfix]
    private static void RollAct1CustomAncients(
        ActModel __instance,
        List<AncientEventModel>? ____sharedAncientSubset)
    {
        // 非 null 即第二幕及之后：那些幕由 BaseLib 的 AddCustomAncientsToPool 负责，不要重复处理。
        if (____sharedAncientSubset != null)
        {
            return;
        }

        if (__instance.Index != 0)
        {
            return;
        }

        if (RoomsField.GetValue(__instance) is not RoomSet rooms || !rooms.HasAncient)
        {
            return;
        }

        AncientEventModel current = rooms.Ancient;

        List<AncientEventModel> candidates = CollectAct1Candidates(__instance);
        if (candidates.Count == 0)
        {
            // 没有可用的自定义先古之民：保持原版抽签结果（涅奥）。
            // 即使涅奥被禁用也在此兜底保留，避免第一幕没有可抽取的先古之民。
            return;
        }

        // 已被强制生成或别的补丁替换成本 Mod 的一层先古之民时不要覆盖。
        if (candidates.Contains(current))
        {
            return;
        }

        List<AncientEventModel> pool = new List<AncientEventModel>();

        // 涅奥被禁用（配置「初始先古之民配置 · 禁用涅奥」）时不再进入抽取池，
        // 第一幕只从本 Mod 的一层先古之民中抽。
        if (!TouhouAncientsConfig.IsBaseGameAncientBanned(current.GetType()))
        {
            pool.Add(current);
        }

        pool.AddRange(candidates);

        // 用派生 RNG 而不是直接消耗传入的 rng（= State.Rng.UpFront）：
        // 直接消耗会改变原版后续幕与地图的随机序列。这里只用运行种子与幕 ID 派生，
        // 各端（主机/客户端）得到同一结果，且不影响原版随机流。
        // 运行种子取自 BaseLib 的 CurrentGeneratingRunState（在 RunManager.GenerateRooms 期间有效）。
        ulong seed = CurrentGeneratingRunState.State?.Rng.Seed ?? 0UL;
        Rng rollRng = new Rng(seed, __instance.Id.Entry);
        rooms.Ancient = rollRng.NextItem(pool) ?? current;
    }

    /// <summary>
    /// 收集本 Mod 中可以在该幕出现的先古之民（<see cref="TouhouAncientBase.ShowAct"/> == 1）。
    /// 结果按 Id.Entry 排序，保证跨端候选顺序一致（抽取依赖列表下标）。
    /// </summary>
    private static List<AncientEventModel> CollectAct1Candidates(ActModel act)
    {
        List<AncientEventModel> result = new List<AncientEventModel>();

        foreach (Type type in ModelDb.AllAbstractModelSubtypes)
        {
            if (type == null || type.IsAbstract || !type.IsSubclassOf(typeof(TouhouAncientBase)))
            {
                continue;
            }

            if (ModelDb.GetByIdOrNull<AncientEventModel>(ModelDb.GetId(type)) is not TouhouAncientBase ancient)
            {
                continue;
            }

            // 只接管第一幕专属的先古之民；ShowAct 为 null 的（二/三层通用）留给 BaseLib。
            if (ancient.ShowAct != 1)
            {
                continue;
            }

            if (TouhouAncientsConfig.IsAncientBanned(type))
            {
                continue;
            }

            if (!ancient.IsValidForAct(act))
            {
                continue;
            }

            if (result.Any(existing => existing.Id == ancient.Id))
            {
                continue;
            }

            result.Add(ancient);
        }

        result.Sort((left, right) => string.Compare(left.Id.Entry, right.Id.Entry, StringComparison.Ordinal));
        return result;
    }
}
