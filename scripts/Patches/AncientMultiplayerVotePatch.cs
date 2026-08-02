using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game.Sync;
using MegaCrit.Sts2.Core.Runs;

namespace TouhouAncients.Scripts.Patches;

/// <summary>
/// 挑战 Ancient（ChallengeEncounter != null，当前为依神姐妹）的多人共享投票结算改写。
///
/// 原版共享事件在全员投票后由房主随机抽一个选项并对全员执行同一选项，导致：
///   - 遗物选项被统一成同一行（全员拿同一个遗物）；
///   - 全员必须选一样才能继续。
///
/// 本补丁将结算改为三种情况：
///   1. 全员投挑战 → 走原版逻辑，全员进入挑战战斗（行为不变）；
///   2. 无人投挑战 → 按玩家分别结算：每个玩家在自己那份事件实例上执行自己投的遗物
///      （各玩家随机池不同，各拿各的；"合作事件"提示下各自独立选行）；
///   3. 混合投票（有人挑战有人遗物）→ 不结算、挂起等待，玩家可改票，直到全员一致
///      （要么全员挑战，要么全员遗物）。
///
/// 说明：
///   - 只有 ChallengeEncounter != null 的 TouhouAncientBase 走此逻辑，其他 Ancient 完全不受影响；
///   - 客户端本地也维护着完整投票表（_playerVotes），房主只需广播一个"按玩家分别结算"的
///     哨兵消息（复用 SharedEventOptionChosenMessage.optionIndex），各端即可自行按玩家结算；
///   - "合作事件"提示（SHARED_EVENT_INFO）+ 各选项上的投票图标就是混合等待时的协调手段，
///     玩家在结算前可反复点选改票（原版共享事件机制）。
/// </summary>
public static class AncientMultiplayerVotePatch
{
    /// <summary>
    /// 哨兵值：表示"按玩家分别结算"，不会与真实选项索引冲突。
    /// 注意：SharedEventOptionChosenMessage.optionIndex 序列化用的是 WriteUInt(optionIndex, 4)，
    /// 只写低 4 位（0~15），因此哨兵值必须落在 4 位可表示范围内；真实选项索引（本事件 0~3）
    /// 远小于 15，取 15 作为哨兵不会冲突。
    /// </summary>
    private const uint PerPlayerResolutionSentinel = 15;

    /// <summary>反射访问 EventSynchronizer 私有成员（事件结算频率低，反射开销可忽略）。</summary>
    private static class SyncAccess
    {
        private static readonly FieldInfo CanonicalEvent = AccessTools.Field(typeof(EventSynchronizer), "_canonicalEvent");
        private static readonly FieldInfo PlayerVotes = AccessTools.Field(typeof(EventSynchronizer), "_playerVotes");
        private static readonly FieldInfo PlayerCollection = AccessTools.Field(typeof(EventSynchronizer), "_playerCollection");
        private static readonly FieldInfo PageIndex = AccessTools.Field(typeof(EventSynchronizer), "_pageIndex");
        private static readonly FieldInfo MessageBuffer = AccessTools.Field(typeof(EventSynchronizer), "_messageBuffer");
        private static readonly FieldInfo NetService = AccessTools.Field(typeof(EventSynchronizer), "_netService");
        private static readonly FieldInfo Events = AccessTools.Field(typeof(EventSynchronizer), "_events");
        private static readonly MethodInfo ChooseOptionForEvent = AccessTools.Method(typeof(EventSynchronizer), "ChooseOptionForEvent")!;

        public static EventModel? GetCanonicalEvent(EventSynchronizer sync) => (EventModel?)CanonicalEvent.GetValue(sync);
        public static List<uint?> GetPlayerVotes(EventSynchronizer sync) => (List<uint?>)PlayerVotes.GetValue(sync)!;
        public static IPlayerCollection GetPlayerCollection(EventSynchronizer sync) => (IPlayerCollection)PlayerCollection.GetValue(sync)!;
        public static List<EventModel> GetEvents(EventSynchronizer sync) => (List<EventModel>)Events.GetValue(sync)!;
        public static uint GetPageIndex(EventSynchronizer sync) => (uint)PageIndex.GetValue(sync)!;
        public static void SetPageIndex(EventSynchronizer sync, uint value) => PageIndex.SetValue(sync, value);
        public static RunLocationTargetedMessageBuffer GetMessageBuffer(EventSynchronizer sync) => (RunLocationTargetedMessageBuffer)MessageBuffer.GetValue(sync)!;
        public static INetGameService GetNetService(EventSynchronizer sync) => (INetGameService)NetService.GetValue(sync)!;
        public static void ChooseOptionForPlayer(EventSynchronizer sync, Player player, int optionIndex) => ChooseOptionForEvent.Invoke(sync, new object[] { player, optionIndex });
    }

    /// <summary>
    /// 房主侧：全员投票后决定结算方式。返回 false 表示已接管，跳过原版。
    /// </summary>
    [HarmonyPatch(typeof(EventSynchronizer), "ChooseSharedEventOption")]
    public static class ChooseSharedEventOption_Patch
    {
        static bool Prefix(EventSynchronizer __instance)
        {
            var canonical = SyncAccess.GetCanonicalEvent(__instance);
            if (canonical is not TouhouAncientBase { ChallengeEncounter: not null })
            {
                return true; // 非挑战 Ancient：原版结算
            }

            int combatIndex = GetCombatOptionIndex(__instance);
            if (combatIndex < 0)
            {
                return true; // 找不到挑战选项：回退原版
            }

            var votes = SyncAccess.GetPlayerVotes(__instance);
            bool anyCombat = votes.Any(v => v == (uint)combatIndex);
            if (!anyCombat)
            {
                // 无人投挑战：按玩家分别结算（广播哨兵消息 + 本地执行）。
                SendPerPlayerResolution(__instance);
                ResolvePerPlayer(__instance);
                return false;
            }
            if (votes.All(v => v == (uint)combatIndex))
            {
                return true; // 全员投挑战：原版（全员进入挑战战斗）。
            }

            // 混合投票（有人挑战有人遗物）：挂起等待，玩家可改票直到全员一致。
            return false;
        }
    }

    /// <summary>
    /// 全员侧：收到哨兵值时按玩家分别结算；否则走原版（全员同一选项）。
    /// 房主在 ChooseSharedEventOption 里直接调用 ResolvePerPlayer，客户端经此补丁进入。
    /// </summary>
    [HarmonyPatch(typeof(EventSynchronizer), "ChooseOptionForSharedEvent")]
    public static class ChooseOptionForSharedEvent_Patch
    {
        static bool Prefix(EventSynchronizer __instance, uint optionIndex)
        {
            if (optionIndex != PerPlayerResolutionSentinel)
            {
                return true; // 原版
            }
            ResolvePerPlayer(__instance);
            return false;
        }
    }

    /// <summary>
    /// 按玩家分别结算：先捕获各玩家投票，再清空投票并推进页面（对齐原版
    /// ChooseOptionForSharedEvent 的顺序），然后为每个玩家在自己那份事件实例上
    /// 执行其投票对应的选项（各自拿到自己随机池里的遗物）。
    /// </summary>
    private static void ResolvePerPlayer(EventSynchronizer sync)
    {
        var votes = SyncAccess.GetPlayerVotes(sync);
        var collection = SyncAccess.GetPlayerCollection(sync);
        var players = collection.Players;

        // 先捕获投票，再清空（原版 ClearPlayerVotes 会把每一项置 null），并推进页面。
        var captured = new uint?[votes.Count];
        for (int i = 0; i < votes.Count; i++)
        {
            captured[i] = votes[i];
            votes[i] = null;
        }
        SyncAccess.SetPageIndex(sync, SyncAccess.GetPageIndex(sync) + 1);

        foreach (var player in players)
        {
            int slot = collection.GetPlayerSlotIndex(player);
            if (slot < 0 || slot >= captured.Length) continue;
            uint? vote = captured[slot];
            if (vote == null) continue;
            SyncAccess.ChooseOptionForPlayer(sync, player, (int)vote.Value);
        }
    }

    /// <summary>广播"按玩家分别结算"的哨兵消息给所有客户端。</summary>
    private static void SendPerPlayerResolution(EventSynchronizer sync)
    {
        var message = new SharedEventOptionChosenMessage
        {
            optionIndex = PerPlayerResolutionSentinel,
            pageIndex = SyncAccess.GetPageIndex(sync),
            location = SyncAccess.GetMessageBuffer(sync).CurrentLocation
        };
        SyncAccess.GetNetService(sync).SendMessage(message);
    }

    /// <summary>
    /// 挑战选项索引：各玩家实例选项结构相同（3 个遗物 + 末尾挑战），挑战选项 textKey 以
    /// ".fight" 结尾（见 TouhouAncientBase.CreateChallengeOption）。取任意玩家实例查找。
    /// </summary>
    private static int GetCombatOptionIndex(EventSynchronizer sync)
    {
        var events = SyncAccess.GetEvents(sync);
        if (events == null || events.Count == 0) return -1;

        var options = events[0].CurrentOptions;
        for (int i = 0; i < options.Count; i++)
        {
            if (options[i].TextKey.EndsWith(".fight"))
            {
                return i;
            }
        }
        // 回退：挑战选项固定追加在末尾。
        return options.Count > 0 ? options.Count - 1 : -1;
    }
}
