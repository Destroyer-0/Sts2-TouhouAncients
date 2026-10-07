using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.cards;

namespace TouhouAncients.Scripts.badges;

/// <summary>打出一张实际耗能达标的极限火花即置位。</summary>
public class ItalianCannonTracker : BadgeModel
{
    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card is MasterSpark && cardPlay.Resources.EnergySpent >= ItalianCannon.MinEnergySpent)
        {
            ItalianCannon.Unlock(cardPlay.Player);
        }

        return Task.CompletedTask;
    }
}
