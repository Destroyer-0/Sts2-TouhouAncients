using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 弹幕的亡灵：敌人的回合结束时，若其进行过攻击但未造成伤害，使其获得2回合缩小。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class DanmukuGhost : TouhouAncientRelics
{
    /// <summary>
    /// 记录本敌人回合中攻击过但未造成伤害的敌人
    /// </summary>
    private List<Creature> attackingEnemiesThisTurn = new();

    /// <summary>
    /// 记录本敌人回合中造成过伤害的敌人（这些敌人不触发缩小）
    /// </summary>
    private List<Creature> damagingEnemiesThisTurn = new();

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("ShrinkAmount", 2m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<ShrinkPower>()];

    /// <summary>
    /// 敌人回合开始时，清空本回合的记录
    /// </summary>
    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side == base.Owner.Creature.Side)
        {
            return Task.CompletedTask;
        }

        attackingEnemiesThisTurn = new List<Creature>();
        damagingEnemiesThisTurn = new List<Creature>();
        return Task.CompletedTask;
    }

    public override Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result,
        ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner.Creature) return Task.CompletedTask;
        if (dealer is not { IsEnemy: true }) return Task.CompletedTask;

        if (result.WasFullyBlocked || result.UnblockedDamage <= 0)
        {
            // 攻击未造成伤害：若该敌人本回合从未造成过伤害，则记入触发列表
            if (!damagingEnemiesThisTurn.Contains(dealer))
            {
                attackingEnemiesThisTurn.Add(dealer);
            }
        }
        else
        {
            // 造成过伤害：该敌人本回合不触发缩小
            attackingEnemiesThisTurn.Remove(dealer);
            if (!damagingEnemiesThisTurn.Contains(dealer))
            {
                damagingEnemiesThisTurn.Add(dealer);
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 敌人回合结束时，给所有攻击过但未造成伤害的敌人施加缩小
    /// </summary>
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side == base.Owner.Creature.Side) return; // 只处理敌人回合结束

        if (attackingEnemiesThisTurn == null || attackingEnemiesThisTurn.Count == 0) return;

        foreach (Creature enemy in attackingEnemiesThisTurn)
        {
            if (!enemy.IsAlive) continue;

            Flash();
            await PowerCmd.Apply<ShrinkPower>(choiceContext, enemy,
                base.DynamicVars["ShrinkAmount"].BaseValue,
                base.Owner.Creature, null);
        }
    }
}