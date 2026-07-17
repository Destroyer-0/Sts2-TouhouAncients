using System.Collections.Generic;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 疫病支票 (The Million Pound Note — Plague Check)
/// 诅咒，1(0)费，保留。
/// 在手牌中时，不能打出费用低于此牌的牌。
/// </summary>
[Pool(typeof(EventCardPool))]
public class TheMillionPoundNote : TouhouAncientCards
{
    private const int energyCost = 0;
    private const CardType type = CardType.Curse;
    private const CardRarity rarity = CardRarity.Ancient;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;

    protected override bool ShouldGlowRedInternal => Owner.PlayerCombatState != null &&
                                                     Owner.PlayerCombatState.Hand.Cards
                                                         .Where(x => x != this && !x.EnergyCost.CostsX)
                                                         .All(x => x.EnergyCost.GetResolved() < EnergyCost.GetResolved());

    public override int MaxUpgradeLevel => 0;

    public override bool CanBeGeneratedByModifiers => false;
    public override bool CanBeGeneratedInCombat => false;

    public TheMillionPoundNote() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(1)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        base.EnergyCost.AddThisCombat(-DynamicVars.Energy.IntValue);
        return base.OnPlay(choiceContext, cardPlay);
    }

    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)
    {
        if (card.Owner != base.Owner) return true;
        if (base.Pile?.Type != PileType.Hand) return true;
        if (card == this) return true;
        if (autoPlayType != AutoPlayType.None) return true;
        if (card.EnergyCost.CostsX) return true;

        return card.EnergyCost.GetResolved() >= base.EnergyCost.GetResolved();
    }
}