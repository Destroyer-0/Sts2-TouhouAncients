using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 铃兰：0费。保留。消耗。
/// 打出时：获得1(2)点能量，抽2张牌。
/// 每当这张牌被保留时，自身获得2层中毒，打出此牌额外获得的能量+1。
/// </summary>
[Pool(typeof(EventCardPool))]
public class LilyBell : TouhouAncientCards
{
    private const int energyCost = 0;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Ancient;
    private const TargetType targetType = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(1),
        new CardsVar(2),
        new DynamicVar("PoisonSelf", 2)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<PoisonPower>(), HoverTipFactory.ForEnergy(this)];

    public LilyBell() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = base.Owner;

        // 获得能量 = 基础能量 + 额外能量
        var totalEnergy = base.DynamicVars.Energy.BaseValue;
        if (totalEnergy > 0)
        {
            await PlayerCmd.GainEnergy(totalEnergy, player);
        }

        // 抽2张牌
        await CardPileCmd.Draw(choiceContext, base.DynamicVars["Cards"].IntValue, player, fromHandDraw: true);
    }


    /// </summary>
    public override async Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != base.Owner.Creature.Side) return;
        if (base.Owner.Creature.CombatState == null) return;
        var player = base.Owner;
        if (Pile is not { Type: PileType.Hand }) return;
        CardCmd.Preview(this, 1);
        foreach (var target in base.Owner.Creature.CombatState.Creatures.Where((Creature c) => !c.IsPet))
        {
            await PowerCmd.Apply<PoisonPower>(target, base.DynamicVars["PoisonSelf"].BaseValue, player.Creature, this);
        }

        base.DynamicVars.Energy.BaseValue += 1;
    }


    /// <summary>
    /// 升级：基础能量从1变为2。
    /// </summary>
    protected override void OnUpgrade()
    {
        base.DynamicVars.Energy.UpgradeValueBy(1m);
    }
}