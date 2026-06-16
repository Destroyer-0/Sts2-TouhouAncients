using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.cardTags;

namespace TouhouAncients.Scripts.Enchantment;

/// <summary>
/// 血池的有机物附魔：将卡牌的费用与辉星消耗降至0，获得重放1。
/// 通过附魔的持久化机制确保读档后修改不丢失。
/// </summary>
public class BloodPond : TouhouAncientEnchantmentModel
{
    public override bool CanBeRandomSelected => false;
    public override bool HasExtraCardText => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.Static(StaticHoverTip.ReplayStatic)];

    /// <summary>
    /// 允许诅咒牌被附魔，供 SkySwallowingSpoon 作为标记用。
    /// </summary>
    public override bool CanEnchant(CardModel card)
    {
        CardPile pile = card.Pile;
        return (pile != null ? (pile.Type == PileType.Deck ? 1 : 0) : 0) == 0 && card.Enchantment == null;
    }

    protected override void OnEnchant()
    {
        // if (Card.Type == CardType.Curse)
        //     return;
        // 费用降至0
        if (Card.EnergyCost.Canonical >= 0)
        {
            Card.EnergyCost.UpgradeBy(-10000);
        }

        // 辉星消耗降至0
        if (Card.CanonicalStarCost > 0)
        {
            Card.TryModifyStarCost(Card, 0, out _);
        }

    }

    public override int EnchantPlayCount(int originalPlayCount)
    {
        if (!Card.CanonicalKeywords.Contains(CardKeyword.Unplayable))
        {
            return originalPlayCount + 1;
        }

        return originalPlayCount;
    }
}