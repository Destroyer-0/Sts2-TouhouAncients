using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Rooms;
using TouhouAncients.Scripts.encounters;

namespace TouhouAncients.Scripts.Patches;

/// <summary>
/// 挑战战斗（<see cref="TouhouAncientEncounter"/>）的奖励相关补丁：
///   默认战斗奖励（金币/卡牌/药水）的抑制改由游戏原生 ModifyRewards 钩子完成——
///   <see cref="TouhouAncientEncounter.TryModifyRewards"/>（在 Entry.Init 用
///   ModHelper.SubscribeForRunStateHooks 注册 Encounter 为 Hook 监听者），不再需要补丁；
///   此处只保留 <see cref="ChallengeVictoryHealPatch"/>。
/// </summary>
public static class ChallengeRewardPatch
{
    /// <summary>挑战胜利后恢复已损失生命值的比例。</summary>
    private const decimal ChallengeHealRatio = 0.5m;

    /// <summary>
    /// 挑战胜利后恢复所有玩家已损失生命值的 50%。Hook.AfterCombatVictory 在战斗胜利存档
    /// （SaveRun）之前触发，故回血会写入存档；读档后不会丢血也不会重复回血。
    /// CreatureCmd.Heal 的 HealInternal 在首个 await 之前同步执行，直接调用即可保证存档前生效
    /// （VFX 等异步部分稍后播放）。
    /// </summary>
    [HarmonyPatch(typeof(Hook), "AfterCombatVictory")]
    public static class ChallengeVictoryHealPatch
    {
        static void Postfix(CombatRoom room)
        {
            if (room.Encounter is not TouhouAncientEncounter { IsChallenge: true })
            {
                return; // 非挑战战斗
            }
            // 每个端都对全部玩家结算：只有主机会写存档，若按 LocalContext.IsMe 只治本地玩家，
            // 主机存档里其他玩家的回血会丢失（读档后掉回未回血）。
            foreach (var player in room.CombatState.Players)
            {
                var lostHp = player.Creature.MaxHp - player.Creature.CurrentHp;
                _ = CreatureCmd.Heal(player.Creature, lostHp * ChallengeHealRatio);
            }
        }
    }
}
