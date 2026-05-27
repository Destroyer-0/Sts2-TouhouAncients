using System.Collections.Generic;
using System.Linq;
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
/// 铃兰：0费。技能。保留。升级后获得固有。
/// 获得1能量，抽2张牌，给予自身0层中毒。
/// 回合结束时，如果这张牌在你的手中，给予所有敌人2（升级后3）层中毒，获得的能量增加1，给予自身的中毒层数+1。
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
        new DynamicVar("PoisonPower", 2),
        new DynamicVar("PoisonSelf", 0)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<PoisonPower>(), HoverTipFactory.ForEnergy(this)];

    public LilyBell() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = base.Owner;

        // 获得能量
        var totalEnergy = base.DynamicVars.Energy.BaseValue;
        if (totalEnergy > 0)
        {
            await PlayerCmd.GainEnergy(totalEnergy, player);
        }

        // 抽2张牌
        await CardPileCmd.Draw(choiceContext, base.DynamicVars["Cards"].IntValue, player, fromHandDraw: true);

        // 给予自身中毒（初始为0，后续回合递增）
        var selfPoison = base.DynamicVars["PoisonSelf"].BaseValue;
        if (selfPoison > 0)
        {
            await PowerCmd.Apply<PoisonPower>(player.Creature, selfPoison, player.Creature, this);
        }
    }

    /// <summary>
    /// 回合结束时若在手牌中：给所有敌人中毒，能量+1，自身上毒量+1
    /// </summary>
    public override async Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != base.Owner.Creature.Side) return;
        if (Pile is not { Type: PileType.Hand }) return;

        CardCmd.Preview(this, 1);

        // 给予所有敌人中毒
        var poisonAmount = base.DynamicVars["PoisonEnemy"].BaseValue;
        var enemies = base.Owner.Creature.CombatState.GetOpponentsOf(base.Owner.Creature).Where(e => e.IsAlive);
        foreach (var enemy in enemies)
        {
            await PowerCmd.Apply<PoisonPower>(enemy, poisonAmount, base.Owner.Creature, this);
        }

        // 获得的能量+1
        base.DynamicVars.Energy.BaseValue += 1;

        // 给予自身的中毒层数+1
        base.DynamicVars["PoisonSelf"].BaseValue += 1;
    }

    /// <summary>
    /// 升级：对敌中毒+1，获得固有（通过CanonicalKeywords处理）。
    /// </summary>
    protected override void OnUpgrade()
    {
        base.DynamicVars["PoisonEnemy"].UpgradeValueBy(1m);
        AddKeyword(CardKeyword.Innate);
    }
}