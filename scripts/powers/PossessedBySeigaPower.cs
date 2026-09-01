using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 走火入魔：霍青娥施加给玩家的负面能力。
/// 每个回合开始时，如果玩家拥有此能力则该回合被霍青娥接管：
/// 立即损失 {HpLoss} 点生命并获得 1 点能量，随后按照费用从高到低、从左到右的顺序
/// 依次自动打出所有可合法打出的手牌（参考耳语耳环的自动打牌逻辑）。
/// </summary>
public class PossessedBySeigaPower : TouhouAncientPowerModel
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("HpLoss", 5m)
    ];

    /// <summary>
    /// 在自动打牌阶段触发，参考 WhisperingEarring 的接管逻辑。
    /// 使用 Late 版本，确保在所有其他自动打牌效果之后执行接管。
    /// </summary>
    public override async Task AfterAutoPrePlayPhaseEnteredLate(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner.Player)
        {
            return;
        }
        if (base.Owner.IsDead)
        {
            return;
        }
        // 本回合才刚刚被添加（例如玩家回合开始阶段死亡被救活），
        // 应等到下个回合才开始接管。
        if (AmountOnTurnStart == 0)
        {
            return;
        }

        Flash();

        // 立即损失 5 点生命（不可格挡、不受力量影响）
        await CreatureCmd.Damage(
            choiceContext,
            base.Owner,
            base.DynamicVars["HpLoss"].BaseValue,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);

        if (base.Owner.IsDead)
        {
            return;
        }

        // 获得 1 点能量
        await PlayerCmd.GainEnergy(1m, player);

        ICombatState combatState = base.Owner.CombatState;
        if (combatState == null)
        {
            return;
        }

        // 按照费用从高到低、从左到右的顺序依次打出可合法打出的手牌。
        // 每打出一张牌后重新排序当前手牌，直到打不出任何合法牌为止。
        while (!CombatManager.Instance.IsOverOrEnding)
        {
            CardModel? card = GetNextPlayableCard(player);
            if (card == null)
            {
                break;
            }

            Creature? target = GetTarget(card, combatState);
            if (card.TargetType == TargetType.AnyEnemy && target == null)
            {
                break;
            }
            if (card.TargetType == TargetType.AnyAlly && target == null)
            {
                break;
            }

            await card.SpendResources();
            await CardCmd.AutoPlay(choiceContext, card, target, AutoPlayType.Default, skipXCapture: true);
        }
    }

    /// <summary>
    /// 从手牌中选出费用最高且当前可合法打出的牌。
    /// 同费用时保持手牌从左到右的顺序。
    /// </summary>
    private CardModel? GetNextPlayableCard(Player player)
    {
        CardPile hand = PileType.Hand.GetPile(player);
        return hand.Cards
            .Select((c, index) => (card: c, index))
            .Where(t => t.card.CanPlay())
            .OrderByDescending(t => t.card.EnergyCost.GetWithModifiers(CostModifiers.All))
            .ThenBy(t => t.card.CurrentStarCost)
            .ThenBy(t => t.index)
            .Select(t => t.card)
            .FirstOrDefault();
    }

    /// <summary>
    /// 为自动打出的牌选择目标：
    /// 敌人取最左侧（可命中敌人中的第一个），友方取随机其他玩家，与耳语耳环一致。
    /// </summary>
    private Creature? GetTarget(CardModel card, ICombatState combatState)
    {
        return card.TargetType switch
        {
            TargetType.AnyEnemy => combatState.HittableEnemies.FirstOrDefault(),
            TargetType.AnyAlly => card.Owner.RunState.Rng.CombatTargets.NextItem(
                combatState.Allies.Where(c => c != null && c.IsAlive && c.IsPlayer && c != card.Owner.Creature)),
            TargetType.AnyPlayer => card.Owner.Creature,
            _ => null,
        };
    }
}
