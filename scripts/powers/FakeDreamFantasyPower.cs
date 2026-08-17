using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.powers;

public class FakeDreamFantasyPower: TouhouAncientPowerModel
{
    public override PowerType Type => PowerType.None;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override bool IsVisibleInternal => false;

    private class Data
    {
        public Dictionary<Creature,decimal> CreatureHpDictionary = new Dictionary<Creature, decimal>();
    }
    protected override object? InitInternalData()
    {
        return new Data();
    }

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if(target == null)return 0;
        if (!GetInternalData<Data>().CreatureHpDictionary.TryGetValue(target, out var value)) return 0;
        return Math.Ceiling(value / 12);
    }

    public void SetCreatureHp(Creature creature, decimal value)
    {
        GetInternalData<Data>().CreatureHpDictionary[creature] = value;
    }
}