using HarmonyLib;
using MegaCrit.Sts2.Core.Rooms;

namespace TouhouAncients.Scripts.Patches;

/// <summary>
/// 挑战战斗读档恢复时的父事件回血修复。
///
/// 原版 Ancient 事件进入时（AncientEventModel.BeforeEventStarted）会把玩家生命回满，
/// 这是"进入先古之民房间恢复血量"的既定机制，必须保留。
///
/// 但挑战战斗是终点（shouldResumeAfterCombat: false），事件不完成；读档恢复时，
/// RunManager.EnterMapPointInternal 会用 new EventRoom(...) 重建挑战战斗的父事件房间
/// （默认非 pre-finished），并以 isPreFinished=false 重新 BeginEvent，导致 BeforeEventStarted
/// 再次全回血——把挑战后写入存档的真实生命覆盖回"挑战前"（即使战斗已经结束）。
///
/// 本补丁在重建父事件房间（isRestoringRoomStackBase=true）时，将其标记为已完成
/// （EventRoom.MarkPreFinished()），使 BeginEvent 以 isPreFinished=true 调用：
/// 原版 isPreFinished=true 时本就不回血，从而保留存档中的真实生命值。
/// 正常进入先古房间（isRestoringRoomStackBase=false）不受影响，全回血保留。
/// </summary>
[HarmonyPatch(typeof(EventRoom), nameof(EventRoom.EnterInternal))]
public static class AncientChallengeRestorePatch
{
    /// <summary>
    /// 仅当重建挑战战斗的父事件房间（读档恢复）时标记为已完成。
    /// 只对启用挑战的 <see cref="TouhouAncientBase"/> 生效（只有它们会走
    /// "挑战战斗终点 + ParentEventId 恢复"路径）；非挑战 Ancient 与原版事件不受影响。
    /// </summary>
    static void Prefix(EventRoom __instance, bool isRestoringRoomStackBase)
    {
        if (!isRestoringRoomStackBase) return;
        if (__instance.CanonicalEvent is TouhouAncientBase { ChallengeEncounter: not null })
        {
            __instance.MarkPreFinished();
        }
    }
}
