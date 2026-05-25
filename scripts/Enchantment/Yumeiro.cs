using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TouhouAncients.Scripts.Enchantment;

/// <summary>
/// 梦色：费用-1，打出后抽一张牌，失去梦色附魔并变化为其他附魔。
/// </summary>
public class Yumeiro : CustomEnchantmentModel
{
    public override bool HasExtraCardText => false;

    public override bool CanEnchant(CardModel card)
    {
        return !card.HasStarCostX && !card.EnergyCost.CostsX && base.CanEnchant(card);
    }

    protected override void OnEnchant()
    {
        if (!HasCard) return;
        // 费用-1
        var newBase = System.Math.Max(0, Card.EnergyCost.Canonical - 1);
        Card.EnergyCost.SetCustomBaseCost(newBase);
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        if (cardPlay?.Card != base.Card) return;
        if (base.Status != EnchantmentStatus.Normal) return;

        var player = base.Card.Owner;

        // 抽一张牌
        await CardPileCmd.Draw(choiceContext, 1, player);

        // 失去梦色附魔并变化为其他附魔
        var availableEnchantments = ModelDb.ActiveEnchantments
            .Where(e => e != base.ModelEntry && e.CanEnchant(base.Card))
            .ToList();

        if (availableEnchantments.Count > 0)
        {
            var rng = player.RunState.Rng.Shuffle;
            var randomEnchantment = availableEnchantments.UnstableShuffle(rng).First();

            // 先禁用当前梦色附魔，再附加新的随机附魔
            base.Status = EnchantmentStatus.Disabled;
            await CardCmd.Enchant(base.Card, randomEnchantment, force: false);
        }
    }
}
