using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 怨仇（对敌人施加的 Debuff）
/// - 敌人对来源（施加者）造成伤害降低 25%
/// - 来源打出攻击牌时，该敌人受到等同于怨仇层数的伤害，层数 -1
/// </summary>
public class YuanChouPower : TouhouAncientPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// 每个施放者（Applier）拥有独立的怨仇实例与层数。
    /// 多人模式（如多个玩家携带不休的恚恨）下，敌人身上会为每个玩家分别显示怨仇图标，
    /// 伤害减伤与攻击反伤仅对各自对应的施放者生效。
    /// </summary>
    public override PowerInstanceType InstanceType => PowerInstanceType.InstancedPerApplier;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new StringVar("Applier")];
    // /// <summary>
    // /// 怨仇的来源（施加该 Debuff 的玩家生物）
    // /// </summary>
    // public Creature? Source { get; set; }

    
    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        ((StringVar)base.DynamicVars["Applier"]).StringValue = applier.Name;
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// 敌人对来源造成的伤害降低 25%
    /// </summary>
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        // 只有敌人（owner）对来源（Source）造成伤害时才减伤
        if (dealer != base.Owner) return 1m;
        if (target != Applier) return 1m;
        if (!props.IsPoweredAttack()) return 1m;
        return 0.75m;
    }

    /// <summary>
    /// 来源（玩家）打出攻击牌时，所有有怨仇的敌人受到伤害
    /// </summary>
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature != base.Applier) return;
        if (cardPlay.Card.Type != CardType.Attack) return;

        Flash();

        // 造成等同于怨仇层数的伤害，然后层数 -1
        await DamageCmd.Attack(Amount)
            .FromCard(cardPlay.Card, cardPlay)
            .Targeting(Owner)
            .Execute(choiceContext);

        await PowerCmd.Decrement(this);
    }
}