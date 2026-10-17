using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace TouhouAncients.Scripts.cards;

/// <summary>阴谋论·异蛇：选中后给玩家 5 张蛇咬。</summary>
[Pool(typeof(StatusCardPool))]
public sealed class ConspiracySerpent : TouhouAncientCards, IConspiracyChoice
{
    private const int energyCost = -1;
    private const CardType type = CardType.Status;
    private const CardRarity rarity = CardRarity.Status;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;

    private const int SnakebiteCount = 5;

    public override bool CanBeGeneratedInCombat => false;

    public override bool CanBeGeneratedByModifiers => false;

    public override int MaxUpgradeLevel => -1;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<Snakebite>()];

    public ConspiracySerpent() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    public async Task OnChosen(PlayerChoiceContext choiceContext, Creature beiko)
    {
        await CardPileCmd.AddToCombatAndPreview<Snakebite>(
            base.Owner.Creature, PileType.Hand, SnakebiteCount, base.Owner, CardPilePosition.Top);
    }
}
