using System;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace TouhouAncients.Scripts.Patches;

/// <summary>
/// 「存档兜底」：别人存档时把"待恢复房间"写空，导致读档重建出一间全新的先古房 —— 这里替它补回去。
///
/// 实测（SAVE-TRACE 抓到的）：海克斯符文选完符文后
/// <c>HextechRunes.HextechRuneSelectionCoordinator.PersistActSelection</c> 调了
/// <c>SaveManager.Instance.SaveRun(null, saveProgress: false)</c>，而 <c>RunManager.ToSave(null)</c> 会
/// <c>PreFinishedRoom = null</c>，把原版 <c>EventRoom.OnEventStateChanged</c> 刚写下的
/// "这间先古房已结算"覆盖掉。之后读档因为 <c>preFinishedRoom == null</c> 走
/// <c>CreateRoom(Event, Ancient)</c>，按 <c>Act.Ancient</c>（哆来咪）重建出一间**全新的先古房**，
/// 而玩家进度（已获得的遗物等）保留在 run state 里 → 于是可以反复拿。
///
/// 做法：给 <c>SaveManager.SaveRun(AbstractRoom?, bool)</c> 加 Prefix，当调用方没带待恢复房间时，
/// 按当前状态把正确的房间**填进参数**（只改这一笔存档写什么，不额外存第二笔，也不会递归）：
///   1. 本地事件实例是「离开梦境」重进出来的涅奥 → 恢复涅奥房（已取祝福则标 pre-finished）；
///   2. 当前房间是已经 pre-finished 的先古 EventRoom（原版已判定结算完成）→ 原样恢复它，
///      读档便会落在 DONE 页而不是一间全新的先古房。
/// 两个条件都不满足就完全不介入：进新节点、开新局、正常游玩中的存档一律照旧。
///
/// 已知边界（按约定不处理）：**已经被写坏的旧存档不会被修好** —— 读它仍会重建全新的先古房，
/// 那一局会多给一次选择；但从那一刻起的每一笔存档都会被这里纠正回正确状态。
/// </summary>
[HarmonyPatch(typeof(SaveManager), "SaveRun", new[] { typeof(AbstractRoom), typeof(bool) })]
internal static class LeaveDreamSaveGuardPatch
{
    /// <summary>
    /// <c>RunManager.State</c> 是 <c>private RunState? State { get; set; }</c>，反射取 getter
    /// （<c>AccessTools.PropertyGetter</c> 返回的是 getter 方法，不是 <c>PropertyInfo</c>）。
    /// </summary>
    private static readonly MethodInfo? StateGetter =
        AccessTools.PropertyGetter(typeof(RunManager), "State");

    /// <summary>
    /// Prefix：调用方没带待恢复房间时，按当前状态补上。用 <c>ref</c> 直接改实参，
    /// 因此这一笔存档写出去的就是正确状态，不需要事后补存一次。
    /// </summary>
    private static void Prefix(ref AbstractRoom? preFinishedRoom)
    {
        try
        {
            if (preFinishedRoom != null)
            {
                return; // 调用方自己给了要恢复的房间，不插手
            }

            var restored = ResolveRoomToRestore();
            if (restored == null)
            {
                return; // 当前状态没有"待恢复"的东西
            }

            preFinishedRoom = restored;
        }
        catch (Exception ex)
        {
            // 兜底失败绝不能影响存档本身。
        }
    }

    /// <summary>按当前运行状态算出"应该写进存档的待恢复房间"；没有就返回 null。</summary>
    private static AbstractRoom? ResolveRoomToRestore()
    {
        var manager = RunManager.Instance;
        var state = GetState();
        if (manager == null || state?.CurrentRoom is not EventRoom currentRoom)
        {
            return null; // 不在事件房里（进节点途中、换幕、地图界面上等）→ 不介入
        }

        // 1) 「离开梦境」重进出来的涅奥：本地事件实例就是那个涅奥 → 要恢复的是涅奥房。
        try
        {
            if (manager.EventSynchronizer.GetLocalEvent() is Neow neow
                && LeaveDreamReentry.IsReentryNeowRoom(neow))
            {
                var neowRoom = new EventRoom(ModelDb.Event<Neow>());
                if (neow.IsFinished)
                {
                    // 祝福已经拿过 → 读档应落在 DONE 页，不能再给一次选择。
                    neowRoom.MarkPreFinished();
                }

                return neowRoom;
            }
        }
        catch
        {
            // 事件同步器里没有事件（例如不在事件房）→ 走下面的分支
        }

        // 2) 已经结算过的先古房：原版 EventRoom.OnEventStateChanged 已经把它标成 pre-finished
        //    （多人时代表全员完成）→ 原样恢复它，读档才会是 DONE 页。
        if (currentRoom.IsPreFinished)
        {
            var resolvedRoom = new EventRoom(currentRoom.CanonicalEvent);
            resolvedRoom.MarkPreFinished();
            return resolvedRoom;
        }

        return null;
    }

    /// <summary>拿当前 RunState；拿不到返回 null。</summary>
    private static RunState? GetState()
    {
        try
        {
            return StateGetter?.Invoke(RunManager.Instance, null) as RunState;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>房间的可读描述，用于日志。</summary>
    private static string Describe(AbstractRoom room)
    {
        try
        {
            return $"{room.RoomType}:{room.ModelId?.Entry ?? "-"}"
                   + (room.IsPreFinished ? "(preFinished)" : "");
        }
        catch (Exception ex)
        {
            return $"<描述失败 {ex.GetType().Name}>";
        }
    }
}
