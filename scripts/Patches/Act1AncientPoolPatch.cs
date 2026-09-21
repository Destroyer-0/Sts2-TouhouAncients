using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
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
/// 实现：Transpiler 在 <c>ActModel.GenerateRooms</c> 里 <c>callvirt GetUnlockedAncients</c> 之后插入一次
/// <see cref="AppendAct1Ancients"/> 调用，把本 Mod 的一层先古之民追加进候选。原版的
/// <c>rng.NextItem</c> 会自动把它们与涅奥同池均匀抽取（各占 1/(N+1)），因此不需要重抽，
/// 也不影响原版随机流。
///
/// **为什么 patch <c>ActModel.GenerateRooms</c> 而不是各幕的 <c>GetUnlockedAncients</c>**：
/// 后者是 abstract、每个幕各一份实现（原版 4 个幕 + 其他 Mod 的幕），想劫持全部幕就得在启动期
/// 反射枚举 <c>ModelDb.AllAbstractModelSubtypes</c>；而前者是基类上的**非抽象**单一实现
/// （<c>public void</c>，子类不可 override），patch 它就等于覆盖所有幕——<c>GetUnlockedAncients</c>
/// 是虚调用，会自然派发到各幕自己的实现，无需知道它们分别在哪。
/// 好处：既不枚举 Mod 类型（避开 <c>ReflectionHelper.ModTypes</c> 在 ModManager 初始化完成前
/// 抛异常的坑，也不会连带中断 <c>Entry.Init()</c> 的后续初始化），也能自动覆盖其他 Mod 的幕；
/// 同时不再依赖挂载时机，无需额外的 Bootstrap 类。
///
/// 只追加本 Mod 自己的 <see cref="TouhouAncientBase.ShowAct"/> == 1 的先古之民，且仅当
/// <c>act.Index == 0</c>（第一幕）时追加；二/三幕与共享池完全不动。追加前按 Id 去重，
/// 别的补丁已注入过的同一个先古之民不会被算两次。
/// </summary>
[HarmonyPatch(typeof(ActModel), nameof(ActModel.GenerateRooms))]
internal static class Act1AncientPoolPatch
{
    /// <summary>
    /// 原版 <c>ActModel.GenerateRooms</c> 里被虚调用的候选来源方法，Transpiler 以它的调用点为锚。
    /// 它是 abstract，所以 IL 里的 <c>callvirt</c> 指向的正是 <see cref="ActModel"/> 上声明的这一个。
    /// </summary>
    private static readonly MethodInfo? GetUnlockedAncientsMethod =
        AccessTools.Method(typeof(ActModel), nameof(ActModel.GetUnlockedAncients), [typeof(UnlockState)]);

    /// <summary>要插入的追加方法，见 <see cref="AppendAct1Ancients"/>。</summary>
    private static readonly MethodInfo? AppendMethod =
        AccessTools.Method(typeof(Act1AncientPoolPatch), nameof(AppendAct1Ancients));

    /// <summary>
    /// 在 <c>callvirt GetUnlockedAncients</c> 之后插入 <c>ldarg.0</c> + <c>call AppendAct1Ancients</c>。
    ///
    /// 该调用点求值后的栈恰好是 <c>[IEnumerable&lt;AncientEventModel&gt;]</c>，紧跟其后压入 <c>this</c>
    /// 就凑成 <see cref="AppendAct1Ancients"/>(<c>candidates, act</c>) 的实参顺序——这也是为什么
    /// 该方法的参数顺序是「候选在前、幕实例在后」。插入位置在 <c>Concat(_sharedAncientSubset)</c> 之前，
    /// 因此 <c>BanAncientPatch</c> 在 <c>Concat</c> 之后插入的过滤逻辑会一并对本 Mod 追加的先古之民生效。
    /// </summary>
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> codes = instructions.ToList();

        if (GetUnlockedAncientsMethod == null || AppendMethod == null)
        {
            Log.Error("[TouhouAncients] Act1AncientPoolPatch: 无法解析 GetUnlockedAncients / AppendAct1Ancients，跳过本次注入。");
            return codes;
        }

        CodeInstruction[] injected =
        [
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Call, AppendMethod)
        ];

        int insertions = 0;

        for (int i = 0; i < codes.Count; i++)
        {
            if (!codes[i].Calls(GetUnlockedAncientsMethod))
            {
                continue;
            }

            codes.InsertRange(i + 1, injected);
            i += injected.Length;
            insertions++;
        }

        if (insertions == 0)
        {
            // 原版 GenerateRooms 的 IL 结构变了（游戏更新）：不中断补丁，只是第一幕不再有本 Mod 先古之民。
            Log.Error("[TouhouAncients] Act1AncientPoolPatch: 未能在 ActModel.GenerateRooms 里定位 GetUnlockedAncients 调用点，第一幕先古之民注入未生效。");
        }
        else
        {
            Log.Info($"[TouhouAncients] Act1AncientPoolPatch: 已在 ActModel.GenerateRooms 注入 {insertions} 处第一幕先古之民追加。");
        }

        return codes;
    }

    /// <summary>
    /// 把本 Mod 的一层先古之民追加进候选列表。只处理第一幕（原版 Overgrowth / Underdocks 的
    /// <c>Index</c> 都是 0），二/三幕与共享池完全不动。
    ///
    /// 参数顺序由 Transpiler 的栈布局决定（候选在前、幕实例在后），不要随意调整。
    /// </summary>
    private static IEnumerable<AncientEventModel> AppendAct1Ancients(IEnumerable<AncientEventModel> candidates, ActModel act)
    {
        List<AncientEventModel> existing = candidates.ToList();

        if (act.Index != 0)
        {
            return existing;
        }

        List<AncientEventModel> extra = CollectAct1Ancients(existing);
        if (extra.Count == 0)
        {
            return existing;
        }

        // 追加在末尾：原版 NextItem 按下标抽取，候选顺序只取决于排序结果，各端一致。
        return existing.Concat(extra);
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
