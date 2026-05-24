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
[Pool(typeof(SharedRelicPool))]
public class DanmukuGhost : TouhouAncientRelics
{
    /// <summary>
    /// 记录本敌人回合中意图攻击的敌人
    /// </summary>
    private Creature? attackingEnemiesThisTurn;

    private bool shouldSmall;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("ShrinkAmount", 2m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<ShrinkPower>()];

    /// <summary>
    /// 敌人回合开始时，记录所有意图攻击的敌人
    /// </summary>
    public override Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side == base.Owner.Creature.Side)
        {
            return Task.CompletedTask;
        }

        attackingEnemiesThisTurn = null;
        shouldSmall = true;
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
            attackingEnemiesThisTurn = dealer;
        }
        else
        {
            attackingEnemiesThisTurn = null;
            shouldSmall = false;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 敌人回合结束时，给攻击未造成伤害的敌人上缩小
    /// </summary>
    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == base.Owner.Creature.Side) return; // 只处理敌人回合结束

        if (!shouldSmall || attackingEnemiesThisTurn == null) return; // 玩家受到了伤害，不触发

        if (attackingEnemiesThisTurn.IsAlive)
        {
            Flash();
            await PowerCmd.Apply<ShrinkPower>(attackingEnemiesThisTurn, base.DynamicVars["ShrinkAmount"].BaseValue,
                base.Owner.Creature, null);
        }
    }
}