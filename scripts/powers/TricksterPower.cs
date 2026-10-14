using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 诈术师：耗能不等于目标费用的攻击牌对因幡帝造成的伤害减半。
/// 目标费用直接存在 Amount（0~3，初始 1），只能由 InabaTewiMonster 用 SetAmount 改写——
/// PowerCmd.ModifyAmount 在 Amount == 0 时会走 ShouldRemoveDueToAmount 把本能力整个移除。
/// </summary>
public class TricksterPower : TouhouAncientPowerModel
{
    private const decimal DamageMultiplier = 0.5m;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (target != base.Owner) return 1m;
        if (cardSource is not { Type: CardType.Attack }) return 1m;
        if (!props.IsPoweredAttack()) return 1m;

        // GetResolved 已把普通牌耗能 clamp 到 ≥0，X 费牌取打出时捕获的 X 值
        return cardSource.EnergyCost.GetResolved() == Amount ? 1m : DamageMultiplier;
    }

    public void ResetAmount(int amount)
    {
        SetAmount(amount,true);
        Flash();
    }
}
