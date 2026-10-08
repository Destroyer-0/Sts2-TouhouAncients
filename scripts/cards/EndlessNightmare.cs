using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 噩梦无终：不可打出的诅咒。抽到时失去 1 点能量；在休息处休息 3 次后从牌组移除。
/// </summary>
[Pool(typeof(EventCardPool))]
public class EndlessNightmare : TouhouAncientCards
{
    private const int RestsToClear = 3;

    public override string? Author => "CAKEMOGO";

    public override bool UseAncientFrame => true;

    public override int MaxUpgradeLevel => 0;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];

    private int _restsRemaining = RestsToClear;

    /// <summary>剩余休息次数，归 0 即从牌组移除。</summary>
    [SavedProperty]
    public int RestsRemaining
    {
        get => _restsRemaining;
        set
        {
            AssertMutable();
            _restsRemaining = value;
            DynamicVars["Rests"].BaseValue = _restsRemaining;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(1),
        new IntVar("Rests", RestsToClear)
    ];

    public EndlessNightmare() : base(-1, CardType.Curse, CardRarity.Curse, TargetType.None, true)
    {
    }

    /// <inheritdoc />
    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card != this) return;

        await Cmd.Wait(0.25f);
        await PlayerCmd.LoseEnergy(base.DynamicVars.Energy.IntValue, base.Owner);
    }

    /// <inheritdoc />
    public override async Task AfterRestSiteHeal(Player player, bool isMimicked)
    {
        if (player != base.Owner || isMimicked) return;

        RestsRemaining -= 1;
        if (RestsRemaining > 0 || base.Pile?.Type != PileType.Deck) return;

        await CardPileCmd.RemoveFromDeck(this);
    }
}
