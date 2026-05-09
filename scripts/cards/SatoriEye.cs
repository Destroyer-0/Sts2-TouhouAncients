using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using TouhouAncients.Scripts.cardTags;
using TouhouAncients.Scripts.CmdUtils;

namespace TouhouAncients.Scripts.cards;

[Pool(typeof(EventCardPool))]
public class SatoriEye : TouhouAncientCards
{
    private const int energyCost = 0;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Ancient;
    private const TargetType targetType = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("SatoriScry", 5),
        new EnergyVar(2)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromKeyword(TouhouAncientKeywords.SatoriScry),
        base.EnergyHoverTip
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    public SatoriEye() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await SatoriScryCmd.SatoriScry(Owner, 5, choiceContext);
        EnergyCost.AddThisTurn(DynamicVars.Energy.IntValue);
    }

    protected override PileType GetResultPileType()
    {
        PileType resultPileType = base.GetResultPileType();
        if (resultPileType != PileType.Discard)
        {
            return resultPileType;
        }

        return PileType.Hand;
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Energy.UpgradeValueBy(-1m);
    }
}