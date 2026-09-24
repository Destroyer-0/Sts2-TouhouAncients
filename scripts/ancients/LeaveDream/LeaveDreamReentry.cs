using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace TouhouAncients.Scripts;

/// <summary>
/// 「离开梦境」：哆来咪事件末尾追加的第 4 个选项。
///
/// 效果：放弃哆来咪的遗物，改为进入涅奥的房间。**逐玩家独立** —— 只替换选择者在本端
/// <c>EventSynchronizer</c> 里的那一份 mutable 事件实例，其他玩家仍然留在哆来咪的选项页上，
/// 互不影响；所有玩家选完之后照常前往下一个节点（由原版地图投票机制带全队走）。
///
/// 为什么不用"压房间"（<c>RunManager.EnterRoomWithoutExitingCurrentRoom</c>）：
/// 压房间是全局操作，多人下必须所有端同时执行，会把全队的事件一起换成涅奥，做不到互不影响；
/// 而"每个玩家各有一份 mutable 事件实例"本来就是 <c>EventSynchronizer</c> 的既有结构
/// （非共享事件下各端 UI 绑定的是自己那一份，见 <c>EventSynchronizer.GetLocalEvent</c>），
/// 所以只换该玩家那一份即可做到互不影响，同时还能复用原版的选项广播通道：
/// 非共享事件下点击选项会执行 <c>ChooseOptionForEvent(本人, index)</c> 并广播
/// <c>OptionIndexChosenMessage</c>，各端收到后也会对同一份实例跑一遍这个选项，
/// 因此该玩家在涅奥房间里的每一次选择都会在所有端复现，不会产生状态分叉
/// （非共享事件在房间退出时会 <c>GenerateChecksum</c>，状态分叉会被判定为 StateDivergence 断线，
/// 所以"只在本端执行"的本地新事件实例是绝对不行的）。
///
/// 存档语义（决定了"绝不会既拿哆来咪遗物又拿涅奥遗物"）：
///   - 未全员完成时的存档发生在进入先古房间那一刻，里面既没有哆来咪遗物、也没有"离开梦境"的选择
///     记录，房间也会被重新生成为哆来咪房 —— 读档等于整段回退，玩家可以自由重选；<br/>
///   - 全员完成时 <c>EventRoom.OnEventStateChanged</c> 会把房间标记 pre-finished 并保存，
///     读档回来是 DONE 页，所有人的选项都只剩"继续"。
/// 因此这里不需要任何"已触发过"的持久标记或选项隐藏逻辑。
/// </summary>
internal static class LeaveDreamReentry
{
    /// <summary>选项名（本地化键为 <c>{AncientId}.pages.INITIAL.options.{OptionName}</c>）。</summary>
    private const string OptionName = "LEAVE_DREAM";

    /// <summary>哆来咪台词："是吗……那么祝您有个愉快的梦醒时分。"</summary>
    public const string DialogueLineKey = "TOUHOUANCIENTS-DOREMY_SWEET_ANCIENT.talk.LEAVE_DREAM.ancient";

    /// <summary>台词上的"继续"按钮文案。</summary>
    public const string DialogueNextKey = "TOUHOUANCIENTS-DOREMY_SWEET_ANCIENT.talk.LEAVE_DREAM.next";

    /// <summary>
    /// 末行哨兵（空文本）。原版规则是"最后一行不显示继续按钮"，所以台词需要第二行占位，
    /// 玩家点"继续"后本类会立刻把这个空气泡隐藏掉。
    /// </summary>
    public const string DialogueSentinelKey = "TOUHOUANCIENTS-DOREMY_SWEET_ANCIENT.talk.LEAVE_DREAM.silent";

    /// <summary>
    /// 是否是「离开梦境」重进出来的涅奥房间：本幕抽中的先古之民是本 Mod 的一层先古之民
    /// （<see cref="TouhouAncientBase.ShowAct"/> == 1），而正在进入的却是原版涅奥 —— 这只可能是本功能造成的。
    /// 回血拦截（<see cref="Patches.LeaveDreamHealSuppressPatch"/>）与完成时存档
    /// （<see cref="Patches.LeaveDreamCompletionSavePatch"/>）共用这一判定。
    /// </summary>
    public static bool IsReentryNeowRoom(AncientEventModel ancient)
    {
        return ancient is Neow && ancient.Owner?.RunState.Act?.Ancient is TouhouAncientBase { ShowAct: 1 };
    }

    /// <summary>创建第 4 个选项（非遗物，选项文本取自 ancients 本地化表）。</summary>
    public static EventOption CreateOption(TouhouAncientBase ancient)
    {
        var textKey = $"{ancient.Id.Entry}.pages.INITIAL.options.{OptionName}";
        return new EventOption(ancient, () => OnChosen(ancient), textKey, Array.Empty<IHoverTip>());
    }

    /// <summary>
    /// 点击「离开梦境」。该回调在所有端都会执行（非共享事件下由选项广播消息驱动），
    /// 因此所有端的换入操作必须完全一致。
    /// </summary>
    private static async Task OnChosen(TouhouAncientBase ancient)
    {
        var player = ancient.Owner;
        if (player == null)
        {
            Log.Error("[TouhouAncients] 离开梦境：事件实例没有归属玩家，已跳过。");
            return;
        }

        var runState = player.RunState;
        var sync = RunManager.Instance.EventSynchronizer;

        // 1) 造一份涅奥实例，替换该玩家槽位上的事件实例。
        //    各端执行顺序、随机派生（Rng 由 run seed + 玩家槽位 + "NEOW" 派生）都一致，
        //    所以各端生成的涅奥选项完全相同，后续按索引广播不会错位。
        var neow = (Neow)ModelDb.Event<Neow>().ToMutable();
        var events = SyncAccess.GetEvents(sync);
        var slot = runState.GetPlayerSlotIndex(player);
        if (slot < 0 || slot >= events.Count)
        {
            Log.Error($"[TouhouAncients] 离开梦境：找不到玩家 {player.NetId} 的事件槽位（{slot}），已跳过。");
            return;
        }

        var replaced = events[slot];
        events[slot] = neow;

        // 房间的"全员完成 → 标记房间 pre-finished + 自动存档"逻辑只订阅了进房时那批实例，
        // 换入的新实例必须补上订阅，否则该玩家是最后一个完成时不会触发存档
        // （多人读档后就不会是"只剩继续"的 DONE 页）。
        TransferRoomSubscription(runState, neow);

        // 2) 旧实例收尾：清掉它对已释放节点/回调的引用。
        replaced.EnsureCleanup();

        // 3) 启动涅奥事件（进房回血由 LeaveDreamHealSuppressPatch 跳过）。
        //    BeginEvent 的第二个参数是事件用的战斗同步器，直接从同步器上取，保持与原版一致。
        await neow.BeginEvent(player, SyncAccess.GetCombatSynchronizer(sync), isPreFinished: false);

        // 4) 单人：立刻保存一次，使读档回到"涅奥的房间"而不是哆来咪的房间。
        //    多人不写（多人读档一定会回到哆来咪的事件房，这是既有行为）。
        if (runState.Players.Count == 1)
        {
            await SaveManager.Instance.SaveRun(new EventRoom(ModelDb.Event<Neow>()));
        }

        // 5) 只有选择者本机需要演出与切换画面；其他端只是持有这份实例用于同步复现。
        if (!LocalContext.IsMe(player))
        {
            return;
        }

        await neow.AfterEventStarted();
        await LeaveDreamSequence.Play(neow, player);
    }

    /// <summary>
    /// 把 <c>EventRoom.OnEventStateChanged</c> 也订阅到换入的新实例上。
    /// 该方法决定了"全员完成即标记房间 pre-finished 并保存"，是读档后只剩"继续"的来源。
    /// </summary>
    private static void TransferRoomSubscription(IRunState runState, EventModel neow)
    {
        if (runState.CurrentRoom is not EventRoom room)
        {
            return;
        }

        var method = AccessTools.Method(typeof(EventRoom), "OnEventStateChanged", new[] { typeof(EventModel) });
        if (method == null)
        {
            Log.Error("[TouhouAncients] 离开梦境：无法解析 EventRoom.OnEventStateChanged，"
                      + "换入的涅奥实例不会触发\"全员完成即标记房间完成\"的存档逻辑。");
            return;
        }

        var handler = (Action<EventModel>)Delegate.CreateDelegate(typeof(Action<EventModel>), room, method);
        neow.StateChanged += handler;
    }

    /// <summary>反射访问 <c>EventSynchronizer</c> 私有成员（事件频率低，反射开销可忽略）。</summary>
    private static class SyncAccess
    {
        private static readonly FieldInfo Events =
            AccessTools.Field(typeof(EventSynchronizer), "_events");

        private static readonly FieldInfo CombatSynchronizer =
            AccessTools.Field(typeof(EventSynchronizer), "_combatSynchronizer");

        public static List<EventModel> GetEvents(EventSynchronizer sync) =>
            (List<EventModel>)Events.GetValue(sync)!;

        public static EventCombatSynchronizer? GetCombatSynchronizer(EventSynchronizer sync) =>
            (EventCombatSynchronizer?)CombatSynchronizer.GetValue(sync);
    }
}
