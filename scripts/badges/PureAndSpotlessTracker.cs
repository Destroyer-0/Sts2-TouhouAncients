using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.Enchantment;

namespace TouhouAncients.Scripts.badges;

/// <summary>每次出牌后检查四个牌堆，全为纯粹附魔即置位。</summary>
public class PureAndSpotlessTracker : BadgeModel
{
    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var card = cardPlay.Card;
        if (!LocalContext.IsMine(card)) return Task.CompletedTask;

        if (card.Owner.PlayerCombatState is { } combatState && AllPure(combatState))
        {
            PureAndSpotless.Unlock(card.Owner);
        }

        return Task.CompletedTask;
    }

    private static bool AllPure(PlayerCombatState combatState)
    {
        var cards = combatState.Hand.Cards
            .Concat(combatState.DrawPile.Cards)
            .Concat(combatState.DiscardPile.Cards)
            .Concat(combatState.ExhaustPile.Cards)
            .Where(c => c.Type is not (CardType.Status or CardType.Curse or CardType.Quest))
            .ToList();

        return cards.Count > 0 && cards.All(c => c.Enchantment is PureLight);
    }
}
