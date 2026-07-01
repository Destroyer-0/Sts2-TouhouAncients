using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 刚欲之证：每回合获得1能量。你不能连续打出TypeLimit张同类型牌。
/// 通过 ShouldPlay 重写实现。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class RigidDesireProof : TouhouAncientRelics
{
    public override string DefaultFileName => "yuuma_default";

    /// <summary>
    /// 最近打出的牌的类型队列，保留最近 (TypeLimit - 1) 条记录。
    /// </summary>
    private readonly Queue<CardType> recentTypeQueue = new();

    public override bool HasUponPickupEffect => false;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(1),
        new DynamicVar("TypeLimit", 3),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.ForEnergy(this),
    ];

    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side == base.Owner.Creature.Side)
        {
            recentTypeQueue.Clear();
        }
        return Task.CompletedTask;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != base.Owner) return Task.CompletedTask;
        recentTypeQueue.Enqueue(cardPlay.Card.Type);
        // 只保留最近 (TypeLimit - 1) 条
        int maxSize = DynamicVars["TypeLimit"].IntValue - 1;
        while (recentTypeQueue.Count > maxSize)
        {
            recentTypeQueue.Dequeue();
        }
        return Task.CompletedTask;
    }

    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        if (player != base.Owner) return amount;
        return amount + DynamicVars.Energy.IntValue;
    }

    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)
    {
        if (card.Owner != base.Owner) return true;
        if (autoPlayType != AutoPlayType.None) return true;

        int limit = DynamicVars["TypeLimit"].IntValue;
        // 队列需满 (limit - 1) 条，且全与当前牌同类型，才阻止
        if (recentTypeQueue.Count < limit - 1) return true;
        if (recentTypeQueue.Any(t => t != card.Type)) return true;

        return false;
    }
}
