using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TouhouAncients.Scripts.Enchantment;

/// <summary>
/// 誓约：此牌被打出后，同时打出其它所有"誓约"牌。
/// 此牌被消耗时，消耗其他所有"誓约"牌。
/// </summary>
public class Oath : CustomEnchantmentModel
{
    /// <summary>
    /// 防止连锁打出时无限循环。
    /// </summary>
    private static readonly HashSet<int> _playingThisChain = new();

    public override bool CanEnchantCardType(CardType cardType)
    {
        // 不能附魔能力牌（丝带蝴蝶结描述为"非能力牌"）
        return cardType != CardType.Power;
    }

    /// <summary>
    /// 打出后，找出场上其他所有带"誓约"附魔的牌并自动打出。
    /// </summary>
    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        if (cardPlay == null) return;
        if (cardPlay.Card != base.Card) return;

        var player = base.Card.Owner;
        if (player?.Creature?.CombatState == null) return;

        // 防止无限循环：如果这张牌已经在播放链中，跳过
        if (!_playingThisChain.Add(base.Card.GetHashCode())) return;

        try
        {
            // 收集所有战斗牌堆中带"誓约"附魔且不是当前牌的卡片
            var oathCards = player.Piles.Where(x=>x.Type is PileType.Draw or PileType.Exhaust or PileType.Discard or PileType.Hand)
                .SelectMany(p => p.Cards)
                .Where(c => c != base.Card && HasOathEnchantment(c))
                .ToList();

            foreach (var oathCard in oathCards)
            {
                // 自动打出
                await CardCmd.AutoPlay(choiceContext, oathCard, target: null);
            }
        }
        finally
        {
            _playingThisChain.Remove(base.Card.GetHashCode());
        }
    }

    /// <summary>
    /// 此牌被消耗时，消耗其他所有"誓约"牌。
    /// 通过 AfterCardChangedPiles 检测牌是否进入了消耗堆。
    /// </summary>
    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        if (!HasCard) return;
        if (card != base.Card) return;

        // 检测是否进入消耗堆（从非消耗堆进入消耗堆）
        if (card.Pile?.Type == PileType.Exhaust && oldPileType != PileType.Exhaust)
        {
            var player = base.Card.Owner;
            if (player?.Creature?.CombatState == null) return;

            // 收集所有战斗牌堆中带"誓约"附魔且不是当前牌的卡片
            var combatState = player.Creature.CombatState;
            var oathCards = player.Piles.Where(x => x.Type is PileType.Draw or PileType.Exhaust or PileType.Discard or PileType.Hand)
                .SelectMany(p => p.Cards)
                .Where(c => c != base.Card && HasOathEnchantment(c))
                .ToList();

            foreach (var oathCard in oathCards)
            {
                await CardCmd.Exhaust(new ThrowingPlayerChoiceContext(), oathCard, skipVisuals: oathCard.Pile is not { Type: PileType.Exhaust });
            }
        }
    }

    /// <summary>
    /// 检查一张牌是否拥有"誓约"附魔。
    /// </summary>
    private static bool HasOathEnchantment(CardModel card)
    {
        return card.Enchantment is Oath;
    }
}
