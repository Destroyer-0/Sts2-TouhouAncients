using System.Threading.Tasks;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 生者必灭之理：3费。能力。4个回合后，清除所有敌人的人工制品并给予9999层灾厄。
/// 每次受到伤害，延长1回合触发时机并给予自身10灾厄。
/// </summary>
[Pool(typeof(EventCardPool))]
public class LifeMustPerish : TouhouAncientCards
{
    private const int energyCost = 3;
    private const CardType type = CardType.Power;
    private const CardRarity rarity = CardRarity.Ancient;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Countdown", 4m),
        new DynamicVar("SelfDoom", 8m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
        [
            HoverTipFactory.FromPower<ArtifactPower>(),
            HoverTipFactory.FromPower<DoomPower>()
        ];

    public LifeMustPerish() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner.Creature.HasPower<LifeMustPerishPower>())
        {
            return;
        }
        // 应用自定义计数 Power
        var power = await PowerCmd.Apply<LifeMustPerishPower>(
            base.Owner.Creature,
            9999,
            base.Owner.Creature,
            this
        );
        power?.SetStartingTurns(base.DynamicVars["Countdown"].IntValue);
    }

    protected override void OnUpgrade()
    {
        // 升级：减少1费
        base.DynamicVars["Countdown"].UpgradeValueBy(-1m);
    }
}
