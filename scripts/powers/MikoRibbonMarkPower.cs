using MegaCrit.Sts2.Core.Entities.Powers;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 巫女蝴蝶结在敌人身上的隐藏标记：Consumed 后本场不再阻止该敌人获得力量。
/// 放在敌人身上而不是遗物里，多人同时持有时共享同一次阻止机会。
/// </summary>
public class MikoRibbonMarkPower : TouhouAncientPowerModel
{
    public override PowerType Type => PowerType.None;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override bool IsVisibleInternal => false;

    public bool Consumed { get; private set; }

    public void Consume()
    {
        Consumed = true;
    }
}
