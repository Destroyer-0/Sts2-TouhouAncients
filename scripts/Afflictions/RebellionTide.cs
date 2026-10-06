using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TouhouAncients.Scripts.Afflictions;

/// <summary>
/// 叛乱之潮：无直接效果，仅作为鬼人正邪「妖器再塑」的征招标记。
/// </summary>
public sealed class RebellionTide : TouhouAncientAfflictionModel
{
    public override bool CanAfflict(CardModel card)
    {
        return card.Rarity == CardRarity.Basic&& (card.Tags.Contains(CardTag.Strike)||card.Tags.Contains(CardTag.Defend));
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card != Card) return;

        CardModel target = Card;
        target.ClearAfflictionInternal();
        await CardCmd.Afflict<ManaRestlessness>(target, 1m);
    }
}
