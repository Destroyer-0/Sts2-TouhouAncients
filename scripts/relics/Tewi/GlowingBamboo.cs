using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using TouhouAncients.Scripts.powers;

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
        base.Status = RelicStatus.Normal;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != base.Owner) return;
        if (_trackedCard != null) return;
        var deckVersion = cardPlay.Card?.DeckVersion;
        if (deckVersion == null || deckVersion.IsUpgraded || !deckVersion.IsUpgradable || deckVersion.HasBeenRemovedFromState) return;

        Flash();
        _trackedCard = cardPlay.Card;
        base.Status = RelicStatus.Disabled;
        InvokeDisplayAmountChanged();
        (await PowerCmd.Apply<GlowingBambooPower>(choiceContext,base.Owner.Creature, 1, base.Owner.Creature, null))?.SetSelectedCard(_trackedCard);
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        var deckVersion = _trackedCard?.DeckVersion;
        if (deckVersion != null
            && PileType.Deck.GetPile(Owner).Cards.Contains(deckVersion)
            && deckVersion is { IsUpgraded: false, IsUpgradable: true, HasBeenRemovedFromState: false })
        {
            Flash();
            CardCmd.Upgrade(deckVersion);
        }

        base.Status = RelicStatus.Normal;
        InvokeDisplayAmountChanged();
        _trackedCard = null;
        return Task.CompletedTask;
    }
}
