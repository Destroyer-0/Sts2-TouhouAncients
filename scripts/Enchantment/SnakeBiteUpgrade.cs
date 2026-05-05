using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TouhouAncients.Scripts.Enchantment;

public class SnakeBiteUpgrade : CustomEnchantmentModel
{
    public override bool CanEnchant(CardModel card)
    {
        return !card.HasStarCostX && !card.EnergyCost.CostsX && base.CanEnchant(card);
    }

    protected override void OnEnchant()
    {
        if (!HasCard) return;
        // 耗能不高于1：如果费用>1，设为1；0或1保持不变
        var currentCost = Card.EnergyCost.GetWithModifiers(CostModifiers.None);
        if (currentCost > 1)
        {
            Card.EnergyCost.SetCustomBaseCost(1);
        }
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (!HasCard) return;
        if (cardPlay.Card != Card) return;

        // 回到手牌
        await CardPileCmd.Add(Card, PileType.Hand, source: this);

        // 本回合随机化耗能（0-3）
        var randomCost = Card.Owner.RunState.Rng.CombatEnergyCosts.NextInt(4);
        Card.EnergyCost.SetThisTurn(randomCost);
    }
}
