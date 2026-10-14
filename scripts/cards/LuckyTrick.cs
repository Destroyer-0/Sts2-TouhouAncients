using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 「幸运」戏法：因幡帝塞进玩家手牌的 0 费状态牌，保留 + 消耗。
/// 打出后本回合随机化手牌耗能（其他「幸运」戏法除外），
/// 并略微偏向抽中诈术师当前的目标费用——凑不出该费用的攻击牌伤害会被减半。
/// </summary>
[Pool(typeof(StatusCardPool))]
public class LuckyTrick : TouhouAncientCards
{
    /// <summary>耗能取值范围 0~3。</summary>
    private const int CostValueCount = 4;

    /// <summary>目标费用的权重，其余 3 个值各占 1（命中率 2/5）。</summary>
    private const int TargetCostWeight = 2;

    private const int energyCost = 0;
    private const CardType type = CardType.Status;
    private const CardRarity rarity = CardRarity.Status;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;

    public override bool CanBeGeneratedByModifiers => false;

    public override bool CanBeGeneratedInCombat => false;

    public override int MaxUpgradeLevel => -1;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, CardKeyword.Retain];

    public LuckyTrick() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player? player = base.Owner;
        if (player?.PlayerCombatState == null) return Task.CompletedTask;

        int? targetCost = FindTargetCost(player);
        foreach (CardModel card in player.PlayerCombatState.Hand.Cards)
        {
            // 其他「幸运」戏法保持 0 费，不参与随机化
            if (card is LuckyTrick) continue;
            if (card.EnergyCost.Canonical < 0) continue;

            card.EnergyCost.SetThisTurn(RollCost(player, targetCost));
        }

        return Task.CompletedTask;
    }

    /// <summary>当前诈术师的目标费用；场上没有诈术师时返回 null（退化为均等随机）。</summary>
    private static int? FindTargetCost(Player player)
    {
        if (player.Creature.CombatState is not { } combatState) return null;

        foreach (Creature enemy in combatState.Enemies)
        {
            if (enemy.GetPower<TricksterPower>() is { } trickster) return trickster.Amount;
        }

        return null;
    }

    private static int RollCost(Player player, int? targetCost)
    {
        if (targetCost is not { } target)
        {
            return player.RunState.Rng.CombatEnergyCosts.NextInt(CostValueCount);
        }

        int roll = player.RunState.Rng.CombatEnergyCosts.NextInt(CostValueCount + TargetCostWeight - 1);
        // 目标费用以外的 3 个值按 (target + 1 + offset) % 4 展开，天然不会落回目标值
        return roll < TargetCostWeight ? target : (target + 1 + roll - TargetCostWeight) % CostValueCount;
    }
}
