using System.Threading.Tasks;
using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Rooms;
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
