using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TouhouAncients.Scripts.cards;

/// <summary>阴谋论·马萨雷斯：选中后给予玩家 1 层「创造性AI」。</summary>
[Pool(typeof(StatusCardPool))]
public sealed class ConspiracyMazares : TouhouAncientCards, IConspiracyChoice
{
    private const int energyCost = -1;
    private const CardType type = CardType.Status;
    private const CardRarity rarity = CardRarity.Status;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;

    public override bool CanBeGeneratedInCombat => false;

    public override bool CanBeGeneratedByModifiers => false;

    public override int MaxUpgradeLevel => -1;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<CreativeAiPower>()];

    public ConspiracyMazares() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    public async Task OnChosen(PlayerChoiceContext choiceContext, Creature beiko)
    {
        await PowerCmd.Apply<CreativeAiPower>(choiceContext, base.Owner.Creature, 1m, base.Owner.Creature, this);
    }
}
