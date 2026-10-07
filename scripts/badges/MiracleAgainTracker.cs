using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.Enchantment;

namespace TouhouAncients.Scripts.badges;

/// <summary>统计本玩家每场战斗打出奇迹附魔牌的张数，达标即置位局内标志。</summary>
public class MiracleAgainTracker : BadgeModel
{
    private int _played;

    public override Task BeforeCombatStart()
    {
        _played = 0;
        return Task.CompletedTask;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Enchantment is not Miracle || !LocalContext.IsMine(cardPlay.Card))
        {
            return Task.CompletedTask;
        }

        if (++_played >= MiracleAgain.RequiredCards)
        {
            MiracleAgain.Unlock(cardPlay.Card.Owner);
        }

        return Task.CompletedTask;
    }
}
