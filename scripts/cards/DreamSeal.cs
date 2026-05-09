using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 梦想封印：1费攻击牌，造成7/10伤害，附加1层易伤、虚弱，使目标本回合临时失去3/5力量。
/// </summary>
[Pool(typeof(EventCardPool))]
public class DreamSeal : TouhouAncientCards
{
    private const int energyCost = 1;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Ancient;
    private const TargetType targetType = TargetType.AnyEnemy;
    private const bool shouldShowInCardLibrary = true;

    private const string _strengthLossKey = "StrengthLoss";
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(7m, ValueProp.Move),
        new PowerVar<DreamSealStrengthDownPower>(3),
        new DynamicVar("StrengthLoss", 5m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<VulnerablePower>(),
        HoverTipFactory.FromPower<WeakPower>(),
        HoverTipFactory.FromPower<StrengthPower>()
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public DreamSeal() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target;
        if (target == null) return;

        // 造成伤害
        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3")
            .Execute(choiceContext);
        // 附加1层易伤
        await PowerCmd.Apply<VulnerablePower>(target, 1m, Owner.Creature, this);

        // 附加1层虚弱
        await PowerCmd.Apply<WeakPower>(target, 1m, Owner.Creature, this);

        // 本回合临时失去力量
        await PowerCmd.Apply<DreamSealStrengthDownPower>(target, base.DynamicVars["DreamSealStrengthDownPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(3m);
        base.DynamicVars.Strength.UpgradeValueBy(2m);
        base.DynamicVars["StrengthLoss"].UpgradeValueBy(2m);
    }
}
