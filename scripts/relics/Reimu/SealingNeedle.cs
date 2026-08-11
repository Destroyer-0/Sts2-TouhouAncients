using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 封魔针：你造成伤害时，给予1虚弱。你对处于虚弱状态的敌人造成伤害增加等同于其虚弱层数的伤害。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class SealingNeedle : TouhouAncientRelics
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<WeakPower>()
    ];

    /// <summary>
    /// 追踪本张攻击牌本次打出时被判定受到攻击的敌人。
    /// 键为打出中的攻击牌，值为已判定目标的集合（HashSet 去重，多段攻击对同一目标只记录一次）。
    /// 仿照 RupturePower 的 BeforeCardPlayed 注册 / AfterCardPlayed 消费机制。
    /// </summary>
    private Dictionary<CardModel, HashSet<Creature>> pendingWeakTargets = new();

    /// <summary>
    /// 模型被克隆（多人联机）时，重置追踪字典。
    /// MutableClone 使用 MemberwiseClone 浅拷贝，若不重置，克隆体与原实例会共享同一个字典引用，导致数据不同步。
    /// </summary>
    protected override void AfterCloned()
    {
        base.AfterCloned();
        pendingWeakTargets = new Dictionary<CardModel, HashSet<Creature>>();
    }

    /// <summary>
    /// 打出前：我方攻击牌注册一个空的目标集合。
    /// </summary>
    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != base.Owner) return Task.CompletedTask;
        if (cardPlay.Card.Type != CardType.Attack) return Task.CompletedTask;

        pendingWeakTargets[cardPlay.Card] = new HashSet<Creature>();
        return Task.CompletedTask;
    }

    /// <summary>
    /// 造成伤害时判定目标（即使未实际造成伤害，如被格挡也会触发）。
    /// 多段攻击会对同一目标多次触发，由 HashSet 去重。
    /// </summary>
    public override Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result,
        ValueProp props, Creature target, CardModel? cardSource)
    {
        if (dealer != base.Owner?.Creature) return Task.CompletedTask;
        if (cardSource == null) return Task.CompletedTask;
        if (!target.IsAlive || !target.IsEnemy) return Task.CompletedTask;
        if (!pendingWeakTargets.TryGetValue(cardSource, out HashSet<Creature> targets)) return Task.CompletedTask;

        targets.Add(target);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 卡牌结算完成后，对本次记录的所有敌人统一施加 1 层虚弱。
    /// 在结算完成后施加，不影响该卡牌自身的伤害与其虚弱增伤。
    /// </summary>
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != base.Owner) return;
        if (cardPlay.Card.Type != CardType.Attack) return;
        if (!pendingWeakTargets.Remove(cardPlay.Card, out HashSet<Creature> targets)) return;

        foreach (Creature target in targets)
        {
            await PowerCmd.Apply<WeakPower>(context, target, 1m, base.Owner.Creature, cardPlay.Card);
        }
    }

    /// <summary>
    /// 对处于虚弱状态的敌人，增加等同于其虚弱层数的伤害值。
    /// </summary>
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (cardSource == null) return 0m;
        if (target == null || !target.IsAlive || !target.IsEnemy) return 0m;
        if (!target.HasPower<WeakPower>()) return 0m;
        if (!props.IsPoweredAttack())
        {
            return 0m;
        }

        if (dealer == base.Owner?.Creature || (dealer?.Monster is Osty && Owner?.Creature == dealer.PetOwner?.Creature))
        {
            return target.GetPowerAmount<WeakPower>();
        }

        return 0m;
    }
}