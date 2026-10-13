using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 阎罗之罪罚：消耗。虚无。被消耗时，从牌组中移除。
/// </summary>
[Pool(typeof(EventCardPool))]
public class YamaPunishment : TouhouAncientCards
{
    public override string? Author => "ジグソーパルル";

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public override int MaxUpgradeLevel => -1;
    public YamaPunishment() : base(1, CardType.Curse, CardRarity.Curse, TargetType.None, true)
    {
    }

    /// <summary>被消耗时把牌组里那一张也移除（战斗副本的 DeckVersion 指向牌组原件）。</summary>
    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (card != this) return;

        var origin = DeckVersion;
        if (origin == null) return;                    // 战斗中生成的副本没有牌组原件
        if (origin.Pile?.Type != PileType.Deck) return;

        await CardPileCmd.RemoveFromDeck(origin, showPreview: false);
    }
}
