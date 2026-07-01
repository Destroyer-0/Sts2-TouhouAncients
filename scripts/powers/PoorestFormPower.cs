using System;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 至贫形态 Power (Counter, 可叠加)
/// 打出耗能为0的牌或以0能量结束回合时，
/// 每1层给予随机一个敌人以下一个Debuff：
///   1 虚弱 / 1 易伤 / 3 中毒 / 8 灾厄
/// （每层可命中不同目标）
/// </summary>
public class PoorestFormPower : TouhouAncientPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// 回合结束时，若剩余能量为 0 则触发
    /// </summary>
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != base.Owner.Side) return;
        if (base.Owner.Player == null) return;

        var combatState = base.Owner.CombatState;
        if (combatState == null) return;

        // 检查是否0能量结束回合
        var playerState = base.Owner.Player.PlayerCombatState;
        if (playerState.Energy != 0) return;

        await ApplyDebuffs(choiceContext);
    }

    /// <summary>
    /// 每层至贫形态给予 1 次随机 Debuff
    /// </summary>
    private async Task ApplyDebuffs(PlayerChoiceContext context)
    {
        var enemies = base.Owner.CombatState?.GetOpponentsOf(base.Owner)
            .Where(c => c.IsAlive)
            .ToList();

        if (enemies == null || enemies.Count == 0) return;

        var rng = base.Owner.Player?.RunState.Rng.CombatCardSelection;
        if (rng == null) return;

        var layerCount = (int)base.Amount;

        for (int i = 0; i < layerCount; i++)
        {
            // 随机选一个存活敌人
            var target = enemies[rng.NextInt(enemies.Count)];

            // 随机选一个 Debuff（4种）
            var debuffType = rng.NextInt(4);
            switch (debuffType)
            {
                case 0:
                    await PowerCmd.Apply<WeakPower>(context, target, 1m, base.Owner, null);
                    break;
                case 1:
                    await PowerCmd.Apply<VulnerablePower>(context, target, 1m, base.Owner, null);
                    break;
                case 2:
                    await PowerCmd.Apply<PoisonPower>(context, target, 3m, base.Owner, null);
                    break;
                case 3:
                    await PowerCmd.Apply<DoomPower>(context, target, 8m, base.Owner, null);
                    break;
            }
        }
    }
}
