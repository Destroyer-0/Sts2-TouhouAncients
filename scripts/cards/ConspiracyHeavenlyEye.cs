using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using TouhouAncients.Scripts.cardTags;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.cards;

/// <summary>阴谋论·天眼：选中后给予玩家「先见之明」（每回合开始预见 3）。</summary>
[Pool(typeof(StatusCardPool))]
public sealed class ConspiracyHeavenlyEye : TouhouAncientCards, IConspiracyChoice
{
    private const int energyCost = -1;
    private const CardType type = CardType.Status;
    private const CardRarity rarity = CardRarity.Status;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;

    private const int ScryAmount = 3;

    public override bool CanBeGeneratedInCombat => false;

    public override bool CanBeGeneratedByModifiers => false;

    public override int MaxUpgradeLevel => -1;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromKeyword(TouhouAncientKeywords.TouhouAncientSatoriScry)];

    public ConspiracyHeavenlyEye() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    public async Task OnChosen(PlayerChoiceContext choiceContext, Creature beiko)
    {
        await PowerCmd.Apply<ConspiracyHeavenlyEyePower>(
            choiceContext, base.Owner.Creature, ScryAmount, base.Owner.Creature, this);
    }
}
