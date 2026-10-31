using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 高塔英雄传：从 3 张升级过的先古之树卡牌中选一张加入手牌，本回合免费打出。
/// </summary>
[Pool(typeof(EventCardPool))]
public class SpireHeroesLegend : TouhouAncientCards
{
    private const int energyCost = 1;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Ancient;
    private const TargetType targetType = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;

    public SpireHeroesLegend() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public override bool CanBeGeneratedByModifiers => false;
    public override bool CanBeGeneratedInCombat => false;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3)];

    /// <summary>先古之树系列（涅奥遗物给的牌）。</summary>
    private static readonly IReadOnlyList<CardModel> TreeCards =
    [
        ModelDb.Card<NeowsFury>(),
        ModelDb.Card<Abundance>(),
        ModelDb.Card<Luminesce>(),
        ModelDb.Card<Relax>(),
        ModelDb.Card<BrightestFlame>(),
        ModelDb.Card<Apotheosis>(),
        ModelDb.Card<Apparition>(),
        ModelDb.Card<Enthralled>(),
        ModelDb.Card<Folly>(),
        ModelDb.Card<Wish>(),
        ModelDb.Card<Maul>(),
        ModelDb.Card<Whistle>()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        List<CardModel> options = TouhouAncientCmd.CreateCombatOptions(
            base.Owner, TreeCards, base.DynamicVars.Cards.IntValue, IsUpgraded);

        await TouhouAncientCmd.ChooseCardsToPile(
            choiceContext, base.Owner, options, 1, base.SelectionScreenPrompt, PileType.Hand, freeThisTurn: true);
    }
}
