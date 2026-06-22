using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 发光竹子：每场战斗你打出的首张未被升级的牌将在战斗结束后被升级。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class GlowingBamboo : TouhouAncientRelics
{
    /// <summary>本场战斗中已标记的首张未升级牌。</summary>
    private CardModel? _trackedCard;

    public override Task BeforeCombatStart()
    {
        _trackedCard = null;
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != base.Owner) return;
        if (_trackedCard != null) return;
        if (cardPlay.Card.IsUpgraded || !cardPlay.Card.IsUpgradable) return;

        _trackedCard = cardPlay.Card;
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        if (_trackedCard != null
            && !_trackedCard.IsUpgraded
            && !_trackedCard.HasBeenRemovedFromState)
        {
            CardCmd.Upgrade(_trackedCard);
            Flash();
        }

        _trackedCard = null;
    }
}
