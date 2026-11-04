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
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.cards;

/// <summary>阴谋论·异蛇：选中后给予玩家「异蛇崇拜」（每回合开始加入一张带消耗的蛇咬）。</summary>
[Pool(typeof(StatusCardPool))]
public sealed class ConspiracySerpent : TouhouAncientCards, IConspiracyChoice
{
    private const int energyCost = -1;
    private const CardType type = CardType.Status;
    private const CardRarity rarity = CardRarity.Status;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;

    public override bool CanBeGeneratedInCombat => false;

    public override bool CanBeGeneratedByModifiers => false;

    public override int MaxUpgradeLevel => -1;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<Snakebite>(), HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];

    public ConspiracySerpent() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    public async Task OnChosen(PlayerChoiceContext choiceContext, Creature beiko)
    {
        await PowerCmd.Apply<ConspiracySerpentPower>(
            choiceContext, base.Owner.Creature, 1m, base.Owner.Creature, this);
    }
}
