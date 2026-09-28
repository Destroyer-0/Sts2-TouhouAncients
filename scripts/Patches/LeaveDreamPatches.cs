using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace TouhouAncients.Scripts.Patches;

/// <summary>
/// 「离开梦境」重进涅奥房间时，不再触发涅奥的进房回血。
///
/// 原版 <c>AncientEventModel.BeforeEventStarted</c> 对 <see cref="Neow"/> 会先把当前生命置 0 再回满
/// （并播放顶栏生命条补间），这是"进入先古之民房间回满血"的既定机制，正常进入时必须保留。
/// 但本功能是"从哆来咪的房间中途走进涅奥的房间"，此时玩家本就是满血，再走一遍回血
/// 既没有意义、也会给"先扣血/扣最大生命换强度，再去涅奥回满"留下口子，所以这里跳过。
///
/// 判定条件（<see cref="LeaveDreamReentry.IsReentryNeowRoom"/>）：
/// 正在进入的是涅奥，且**本幕抽中的先古之民是本 Mod 的一层先古之民**（<c>ShowAct == 1</c>）。
/// 一层先古之民房里只会出现哆来咪，所以"本幕先古是哆来咪、进的却是涅奥"只可能是本功能造成的；
/// 该条件不依赖任何运行期标记，因此读档重建涅奥房间时同样生效。
/// </summary>
[HarmonyPatch(typeof(AncientEventModel), "BeforeEventStarted", new[] { typeof(bool) })]
internal static class LeaveDreamHealSuppressPatch
{
    /// <summary>
    /// 命中时跳过整个原版方法。注意 <c>BeforeEventStarted</c> 返回 <see cref="Task"/>，
    /// 跳过时必须显式给出已完成的 Task，否则调用方 <c>await</c> 到 null 会抛空引用。
    /// </summary>
    private static bool Prefix(AncientEventModel __instance, ref Task __result)
    {
        if (!LeaveDreamReentry.IsReentryNeowRoom(__instance))
        {
            return true;
        }

        __result = Task.CompletedTask;
        return false;
    }
}

/// <summary>
/// 「离开梦境」的告别台词需要玩家点「继续」才能推进。
///
/// 不自己去找对话推进热区（<c>NAncientDialogueHitbox : NButton</c>，是布局场景里的私有节点），
/// 而是直接给原版的推进方法加 Postfix：只有本功能正在等推进时才通知演出继续，
/// 其他对话（先古之民入场台词、各个事件的分支对话）完全不受影响。
/// </summary>
[HarmonyPatch(typeof(NAncientEventLayout), "OnDialogueHitboxClicked")]
internal static class LeaveDreamDialogueAdvancePatch
{
    private static void Postfix()
    {
        LeaveDreamSequence.NotifyDialogueAdvanced();
    }
}

/// <summary>
/// 「离开梦境」线在涅奥房间里完成选择后的存档。
///
/// 单人模式下：玩家选定涅奥的祝福后，把存档的房间改成"已完成（pre-finished）的涅奥房间"，
/// 这样再读档时房间是 DONE 页、选项只剩"继续"，与多人模式"全员完成后只剩继续"的表现一致
/// （也避免反复读档重复挑祝福）。
///
/// 多人模式不写（多人读档一定会回到哆来咪的事件房，是既有行为）。
/// </summary>
[HarmonyPatch(typeof(AncientEventModel), "Done")]
internal static class LeaveDreamCompletionSavePatch
{
    private static void Postfix(AncientEventModel __instance)
    {
        if (!LeaveDreamReentry.IsReentryNeowRoom(__instance))
        {
            return;
        }

        var player = __instance.Owner;
        if (player == null || player.RunState.Players.Count > 1)
        {
            return;
        }

        var room = new EventRoom(ModelDb.Event<Neow>());
        room.MarkPreFinished();
        TaskHelper.RunSafely(SaveManager.Instance.SaveRun(room));
    }
}

/// <summary>
/// 「离开梦境」重进出来的涅奥在"本局带 modifier"时拿不到任何初始选项的兜底。
///
/// 原版 <c>Neow.GenerateInitialOptions</c> 是二选一的岔路：
///   - <c>RunState.Modifiers</c> 为空 → 正常掷出 3 个选项（这段里没有任何"返回空"的出口）；
///   - 非空 → 只收集"各 modifier 自己提供的涅奥选项"（<c>ModifierModel.GenerateNeowOption</c>，
///     基类默认返回 null），一个都没有时 <c>return Array.Empty&lt;EventOption&gt;()</c>。
/// 空列表会让 <c>AncientEventModel.SetInitialEventState</c> 直接走 <c>StartPreFinished()</c>：
/// 事件当场判为已完成，玩家只看到「继续」（PROCEED），一个祝福都拿不到。
///
/// 原版自己碰不到这条空路径：涅奥只出现在第一幕第一间房，而第一间房的选项是在
/// <c>EventRoom.EnterInternal</c> → <c>EventSynchronizer.BeginEvent</c> 里生成的，
/// 之后才跑 <c>Hook.AfterRoomEntered</c>；而"带 modifier 但没登记在 run 里"的 mod
/// （例：海克斯符文的 Mayhem 在 <c>OnRoomEntered</c> 里补挂 modifier，日志中可见
/// <c>recovered missing modifier room=EventRoom ... reattaching</c>）正是那时才把它写进 run 的。
/// 所以第一间房是涅奥时表还空着（正常三选一），而「离开梦境」是在**第一间房之内**才新建涅奥，
/// 那时 modifier 已经登记 → 必然踩中空路径：单机、联机都会出现"选了离开梦境，涅奥房间只剩继续"。
///
/// 兜底：临时把 <c>RunState.Modifiers</c> 换成空表，让原版再跑一遍同一个方法（于是走"没有 modifier"
/// 那条正常岔路，掷出与"第一间房恰好是涅奥"时一致的标准三选一），跑完在 <c>finally</c> 里立刻换回来。
/// 三个条件同时满足才动手：结果是空、是本功能重进出来的涅奥、本局 modifier 表非空。
/// modifier 自己提供了涅奥选项时（原版 Draft / Sealed Deck / Insanity / All Star / Specialized）
/// 结果非空，本补丁完全不介入；反射解析失败时也不介入，只打错误日志、维持现状。
/// </summary>
[HarmonyPatch(typeof(Neow), "GenerateInitialOptions")]
internal static class LeaveDreamNeowOptionsPatch
{
    /// <summary>回退时要再调一次被打补丁的原版方法，用它挡住递归。</summary>
    [ThreadStatic] private static bool _rerolling;

    /// <summary>原版涅奥的选项生成（override，非 public，用名字取）。</summary>
    private static readonly MethodInfo? GenerateInitialOptions =
        AccessTools.Method(typeof(Neow), "GenerateInitialOptions");

    /// <summary><c>RunState.Modifiers</c> 是 <c>{ get; private set; }</c>，优先反射 setter。</summary>
    private static readonly MethodInfo? ModifiersSetter =
        AccessTools.PropertySetter(typeof(RunState), nameof(RunState.Modifiers));

    /// <summary>setter 解析不到时退回自动属性后备字段。</summary>
    private static readonly FieldInfo? ModifiersBackingField =
        AccessTools.Field(typeof(RunState), "<Modifiers>k__BackingField");

    private static void Postfix(Neow __instance, ref IReadOnlyList<EventOption> __result)
    {
        if (_rerolling || __result.Count > 0)
        {
            return; // 原版已经给了选项（含 modifier 自己提供的那个），不要动
        }

        if (!LeaveDreamReentry.IsReentryNeowRoom(__instance))
        {
            return; // 只处理「离开梦境」重进出来的涅奥；正常涅奥不归本功能管
        }

        var runState = __instance.Owner?.RunState;
        if (runState is not RunState state || state.Modifiers.Count == 0)
        {
            return; // 表本来就是空的却仍给回空选项：不是本问题，别乱改
        }

        if (GenerateInitialOptions == null || (ModifiersSetter == null && ModifiersBackingField == null))
        {
            Log.Error("[TouhouAncients] 离开梦境：无法解析 Neow.GenerateInitialOptions / RunState.Modifiers，"
                      + "涅奥的初始选项为空时无法回退到标准三选一（玩家只会看到「继续」）。");
            return;
        }

        var modifiers = state.Modifiers;
        if (!TrySetModifiers(state, Array.Empty<ModifierModel>()))
        {
            Log.Error("[TouhouAncients] 离开梦境：无法临时清空 RunState.Modifiers，"
                      + "涅奥的初始选项仍为空（玩家只会看到「继续」）。");
            return;
        }

        try
        {
            _rerolling = true;
            __result = (IReadOnlyList<EventOption>)GenerateInitialOptions.Invoke(__instance, null)!;
        }
        catch (Exception ex)
        {
            // 保持原样（空选项）比让异常冒出去打断事件初始化好。
            var inner = (ex as TargetInvocationException)?.InnerException ?? ex;
            Log.Error($"[TouhouAncients] 离开梦境：按「没有 modifier」重掷涅奥选项失败，保持原样：{inner}");
        }
        finally
        {
            if (!TrySetModifiers(state, modifiers))
            {
                Log.Error("[TouhouAncients] 离开梦境：RunState.Modifiers 没能还原，本局后续的 modifier 逻辑可能失效。");
            }

            _rerolling = false;
        }

        if (__result.Count > 0)
        {
            Log.Warn($"[TouhouAncients] 离开梦境：modifier（{string.Join(", ", modifiers.Select(m => m.Id.Entry))}）"
                     + $"不提供涅奥选项，已按「无 modifier」重掷 {__result.Count} 个。");
        }
    }

    /// <summary>
    /// 把 <c>RunState.Modifiers</c> 换成指定列表；setter 与后备字段都拿不到（或调用失败）时返回 false。
    /// 调用方保证只在同步代码里用它，不跨 await。
    /// </summary>
    private static bool TrySetModifiers(RunState state, IReadOnlyList<ModifierModel> modifiers)
    {
        try
        {
            if (ModifiersSetter != null)
            {
                ModifiersSetter.Invoke(state, new object[] { modifiers });
                return true;
            }

            if (ModifiersBackingField != null)
            {
                ModifiersBackingField.SetValue(state, modifiers);
                return true;
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[TouhouAncients] 离开梦境：写入 RunState.Modifiers 失败：{ex}");
        }

        return false;
    }
}
