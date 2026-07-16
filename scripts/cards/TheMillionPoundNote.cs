using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 疫病支票 (The Million Pound Note — Plague Check)
/// 1(0)费，保留，消耗。
/// 移除名流状态，消耗所有债务，每消耗一张失去5金币。
/// </summary>
[Pool(typeof(EventCardPool))]
public class TheMillionPoundNote : TouhouAncientCards
{
    private const int energyCost = 1;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Ancient;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new GoldVar(5),
        new CalculationBaseVar(0m),
        new CalculationExtraVar(1m),
        new CalculatedVar("CalculatedHits").WithMultiplier((CardModel card, Creature? _) =>
            card.Owner?.PlayerCombatState != null
                ? card.Owner.PlayerCombatState.AllCards.Count(c => c is Debt && c is { HasBeenRemovedFromState: false, Pile: not { Type: PileType.Exhaust } }) * 5
                : 0)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<Debt>()
            .Append(HoverTipFactory.FromPower<TheMillionPoundNotePower>());

    public TheMillionPoundNote() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var power = Owner.Creature.GetPower<TheMillionPoundNotePower>();
        if (power != null)
        {
            await PowerCmd.Remove(power);
        }

        var debts = Owner.PlayerCombatState.AllCards.Where(c => c is Debt && c is { HasBeenRemovedFromState: false, Pile: not { Type: PileType.Exhaust } }).ToList();

        foreach (var debt in debts)
        {
            await CardCmd.Exhaust(choiceContext, debt);
        }

        if (debts.Count > 0)
        {
            await PlayerCmd.LoseGold(((CalculatedVar)base.DynamicVars["CalculatedHits"]).Calculate(Owner.Creature), Owner);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}