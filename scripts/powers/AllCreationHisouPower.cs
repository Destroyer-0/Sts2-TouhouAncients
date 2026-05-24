using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace TouhouAncients.Scripts.powers;

public class AllCreationHisouPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyPowerAmountGiven(PowerModel power, Creature giver, decimal amount, Creature? target,
        CardModel? cardSource)
    {
        if (target != null && target == Owner && power is not AllCreationHisouPower && power.Type is PowerType.Buff &&
            power.StackType is PowerStackType.Counter)
        {
            amount++;
        }

        return base.ModifyPowerAmountGiven(power, giver, amount, target, cardSource);
    }
}