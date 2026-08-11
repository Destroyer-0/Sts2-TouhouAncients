using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace TouhouAncients.Scripts.encounters;

/// <summary>
/// 东方角色挑战战斗的 Encounter 基类，统一处理自定义 BGM。
///
/// 子类只需重写 <see cref="BgmFileName"/> 指定自己的 BGM 文件，战斗开始/结束时由
/// <see cref="EncounterBgm"/>（scripts/EncounterBgm.cs）自动播放/停止——Encounter
/// 本身不接收战斗 Hook，故由全局订阅 CombatManager 事件的 EncounterBgm 统一处理。
/// </summary>
public abstract class TouhouAncientEncounter : CustomEncounterModel
{
    protected TouhouAncientEncounter(RoomType roomType) : base(roomType)
    {
    }

    /// <summary>
    /// 是否为挑战战斗（由 Ancient 事件的挑战选项进入）。挑战战斗不生成默认战斗奖励
    /// （金币/卡牌/药水），奖励只含 StartChallenge 传入 CombatRoom.ExtraRewards 的挑战遗物。
    /// 由 <see cref="TouhouAncientBase.StartChallenge"/> 在 canonical 实例上置位，游戏内部
    /// ToMutable（MemberwiseClone 拷贝字段）克隆战斗实例时继承该标志；调用返回后
    /// StartChallenge 会在 finally 中复位 canonical，故该标志只标记"这一场"经挑战进入的战斗。
    /// </summary>
    public bool IsChallenge { get; set; }

    /// <summary>
    /// 自定义 BGM 文件名（位于 res://debug_audio/，也可填完整的 res:// 路径）。
    /// 返回 null 表示不播放自定义 BGM。注意：对应音频文件的 import 需开启 loop=true 才能循环。
    /// </summary>
    public virtual string? BgmFileName => null;

    /// <summary>
    /// 挑战战斗不生成默认战斗奖励（金币/卡牌/药水）：通过游戏原生 ModifyRewards 钩子
    /// （在 <see cref="Entry.Init"/> 用 ModHelper.SubscribeForRunStateHooks 把当前挑战
    /// Encounter 注册进 RunState Hook 监听者）在奖励生成时只保留 StartChallenge 传入
    /// CombatRoom.ExtraRewards 的挑战遗物。按类型判断（本基类只用于挑战战斗），读档重建后
    /// 依然生效，无需持久化 IsChallenge。正常/读档奖励流程（OfferRoomEndRewards →
    /// RewardsSet.GenerateWithoutOffering → Hook.ModifyRewards）都会调用。
    /// </summary>
    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (room is not CombatRoom combatRoom) return false;
        if (!combatRoom.ExtraRewards.TryGetValue(player, out var challengeRewards)) return false;
        int before = rewards.Count;
        rewards.RemoveAll(r => !challengeRewards.Contains(r));
        return rewards.Count != before;
    }
}
