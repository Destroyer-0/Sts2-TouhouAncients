using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.Enchantment;

/// <summary>
/// 纯粹：打出后，清除所有手牌的附魔并全部附魔纯粹；这张牌的伤害/格挡额外 +手牌中纯粹牌数。
/// </summary>
public class PureLight : TouhouAncientEnchantmentModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Count", 0)];

    // 手牌变动时原版会遍历所有牌回调这里，卡面「至纯N」靠它刷新
    public override void RecalculateValues()
    {
        if (!HasCard) return;

        base.DynamicVars["Count"].BaseValue = PureLightInHand();
    }

    public override decimal EnchantDamageAdditive(decimal originalDamage, ValueProp props)
    {
        if (!props.IsPoweredAttack()) return 0m;

        return PureLightInHand();
    }

    // 附魔的格挡 hook 本身不区分 Unpowered，原版灵巧（Nimble）也是直接返回
    public override decimal EnchantBlockAdditive(decimal originalBlock)
    {
        return PureLightInHand();
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        if (cardPlay?.Card != base.Card) return;

        var hand = base.Card.Owner.PlayerCombatState?.Hand;
        if (hand == null) return;

        // 复制一份，避免附魔过程中改动集合
        foreach (var card in hand.Cards.ToList())
        {
            // 本牌已在打出流程里，再附魔会干扰结算
            if (card == base.Card) continue;
            if (card.Enchantment is PureLight) continue;

            // Enchant 遇到异种附魔会抛异常，必须先清
            if (card.Enchantment != null) CardCmd.ClearEnchantment(card);

            if (ModelDb.Enchantment<PureLight>().CanEnchant(card))
            {
                CardCmd.Enchant<PureLight>(card, 1m);
            }
        }
    }

    /// <summary>手牌里带纯粹的牌数；本牌打出后已离手，补上这 1 点卡面数值才和实际结算一致。</summary>
    private int PureLightInHand()
    {
        if (!HasCard) return 0;

        var hand = base.Card.Owner?.PlayerCombatState?.Hand;
        if (hand == null) return 0;

        int count = hand.Cards.Count(c => c.Enchantment is PureLight);
        if (!hand.Cards.Contains(base.Card)) count++;

        return count;
    }
}
