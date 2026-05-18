using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.cards;

namespace TouhouAncients.Scripts.powers;

public class MagicWalletPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;


    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (ShouldSkip(card))
        {
            return false;
        }

        modifiedCost = default(decimal);
        return true;
    }

    public override bool TryModifyStarCost(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (ShouldSkip(card))
        {
            return false;
        }

        modifiedCost = default(decimal);
        return true;
    }


    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature == base.Owner && !cardPlay.IsAutoPlay &&
            cardPlay.IsLastInSeries && !ShouldSkip(cardPlay.Card))
        {
            await PowerCmd.Decrement(this);
        }
    }

    private bool ShouldSkip(CardModel card)
    {
        bool flag = card.Owner.Creature != base.Owner;
        bool flag2 = flag;
        if (card is MagicWallet)
        {
            return true;
        }
        if (!flag2)
        {
            bool flag3;
            switch (card.Pile?.Type)
            {
                case PileType.Hand:
                case PileType.Play:
                    flag3 = true;
                    break;
                default:
                    flag3 = false;
                    break;
            }

            flag2 = !flag3;
        }

        if (!flag2)
        {
            return false;
        }

        return true;
    }
}