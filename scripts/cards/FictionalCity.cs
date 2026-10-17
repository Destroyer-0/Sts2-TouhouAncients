using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.cards;

/// <summary>虚构都市：贝子塞入抽牌堆的状态牌，抽到时本回合失去力量与敏捷。</summary>
[Pool(typeof(StatusCardPool))]
public sealed class FictionalCity : TouhouAncientCards
{
    private const int energyCost = -1;
    private const CardType type = CardType.Status;
    private const CardRarity rarity = CardRarity.Status;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;

    public override bool CanBeGeneratedInCombat => false;

    public override bool CanBeGeneratedByModifiers => false;

    public override int MaxUpgradeLevel => -1;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Unplayable, CardKeyword.Ethereal];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("StrengthLoss", 1),
        new DynamicVar("DexterityLoss", 1)
    ];

    public FictionalCity() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card != this) return;

        await Cmd.Wait(0.25f);
        await PowerCmd.Apply<FictionalCityStrengthDownPower>(choiceContext, base.Owner.Creature,
            base.DynamicVars["StrengthLoss"].BaseValue, base.Owner.Creature, this);
        await PowerCmd.Apply<FictionalCityDexterityDownPower>(choiceContext, base.Owner.Creature,
            base.DynamicVars["DexterityLoss"].BaseValue, base.Owner.Creature, this);
    }
}
