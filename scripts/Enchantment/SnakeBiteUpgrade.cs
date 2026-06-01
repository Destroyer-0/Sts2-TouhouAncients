using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TouhouAncients.Scripts.Enchantment;

public class SnakeBiteUpgrade : TouhouAncientEnchantmentModel
{
    public override bool HasExtraCardText => true;
    private int _shouldResetNum;
    public override Task BeforeCombatStart()
    {
        _shouldResetNum = 0;
        return base.BeforeCombatStart();
    }

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
            _shouldResetNum++;
        }
        base.Card.EnergyCost.AddUntilPlayed(-1);
        return Task.CompletedTask;
    }

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        if (!HasCard) return;
        if (card != base.Card)
        {
            return ;
        }
        
        if (oldPileType == PileType.Hand)
        {
            base.Card.EnergyCost.AddUntilPlayed(_shouldResetNum);
            _shouldResetNum = 0;
        }

        if (oldPileType == PileType.Play && card.Pile != null && card.Pile.Type != PileType.None)
        {
            await CardPileCmd.Add(Card, PileType.Hand, source: this);

            // 本回合随机化耗能（0-3）
            var randomCost = Card.Owner.RunState.Rng.CombatEnergyCosts.NextInt(4);
            Card.EnergyCost.SetThisTurn(randomCost);
        }
        
        return;
    }

    // public override async Task AfterCardPlayedLate(PlayerChoiceContext context, CardPlay cardPlay)
    // {
    //     if (!HasCard) return;
    //     if (cardPlay.Card != Card) return;
    //     if (Card.HasBeenRemovedFromState) return;
    //     // 回到手牌
    //     await CardPileCmd.Add(Card, PileType.Hand, source: this);
    //
    //     // 本回合随机化耗能（0-3）
    //     var randomCost = Card.Owner.RunState.Rng.CombatEnergyCosts.NextInt(4);
    //     Card.EnergyCost.SetThisTurn(randomCost);
    // }
}
