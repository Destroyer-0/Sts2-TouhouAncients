using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TouhouAncients.Scripts.Enchantment;

/// <summary>
/// 梦色：费用-1，打出后抽一张牌，失去梦色附魔并变化为其他附魔。
/// </summary>
public class Yumeiro : CustomEnchantmentModel
{
    public override bool HasExtraCardText => false;

    // public override bool CanEnchant(CardModel card)
    // {
    //     return !card.HasStarCostX && !card.EnergyCost.CostsX && base.CanEnchant(card);
    // }

    protected override void OnEnchant()
    {
        if (!HasCard) return;
        if (Card.EnergyCost.CostsX) return;
        Card.EnergyCost.UpgradeBy(-1);
    }

    private static List<EnchantmentModel>? s_allEnchantments;

    private static List<EnchantmentModel> AllEnchantments
    {
        get
        {
            s_allEnchantments ??= ModelDb.DebugEnchantments.ToList();

            return s_allEnchantments;
        }
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        if (cardPlay?.Card != base.Card) return;
        if (base.Status != EnchantmentStatus.Normal) return;

        var player = base.Card.Owner;

        // 抽一张牌
        await CardPileCmd.Draw(choiceContext, 1, player);

        base.Status = EnchantmentStatus.Disabled;
        if (Card.HasBeenRemovedFromState) return;
        CardCmd.ClearEnchantment(Card);
        // 失去梦色附魔并变化为其他附魔
        var availableEnchantments = AllEnchantments
            .Where(e =>
            {
                if (e is Bloodshed) return false;
                if (e is Yumeiro) return false;
                return e.CanEnchant(cardPlay?.Card);
            })
            .ToList();

        if (availableEnchantments.Count > 0)
        {
            var randomEnchantment =
                availableEnchantments.UnstableShuffle(player.RunState.Rng.CombatCardGeneration).First();
            // 附加新的随机附魔
            CardCmd.Enchant(randomEnchantment.ToMutable(), cardPlay?.Card, 1m);
        }
    }
}