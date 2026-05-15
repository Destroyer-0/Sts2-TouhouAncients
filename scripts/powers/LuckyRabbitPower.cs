using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 幸运白兔的增益：费用为0的卡牌造成伤害与提供格挡翻倍。
/// </summary>
public class LuckyRabbitPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.Static(StaticHoverTip.Block)];

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (dealer != base.Owner) return 1m;
        if (cardSource == null) return 1m;
        if (!props.IsPoweredAttack()) return 1m;
        if (cardSource.EnergyCost.GetResolved() != 0) return 1m;
        return base.Amount;
    }

    public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (target != base.Owner) return 1m;
        if (cardSource == null) return 1m;
        if (!props.IsCardOrMonsterMove()) return 1m;
        if (cardSource.EnergyCost.GetResolved() != 0) return 1m;
        return base.Amount;
    }
}