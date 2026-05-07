using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TouhouAncients.Scripts.Enchantment;

public class SnakeBiteUpgrade : CustomEnchantmentModel
{
    private int shouldResetNum;
    public override Task BeforeFlush(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Card.Owner)
        {
            return Task.CompletedTask;
        }

        if (Card.EnergyCost.CostsX)
        {
            return Task.CompletedTask;
        }
        CardPile? pile = base.Card.Pile;
        if (pile == null || pile.Type != PileType.Hand)
        {
            return Task.CompletedTask;
        }

        if (base.Card.EnergyCost.GetAmountToSpend() > 0)
        {
            shouldResetNum++;
        }
        base.Card.EnergyCost.AddUntilPlayed(-1);
        return Task.CompletedTask;
    }

    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        if (card != base.Card)
        {
            return Task.CompletedTask;
        }
        
        if (oldPileType == PileType.Hand)
        {
            base.Card.EnergyCost.AddUntilPlayed(shouldResetNum);
            shouldResetNum = 0;
        } 
        return Task.CompletedTask;
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
