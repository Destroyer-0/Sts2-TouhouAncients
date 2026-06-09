using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 启动资金 (Capital Power)
/// 纯数值显示用途，储存当前的启动资金量。
/// 由 RichestFormPower 在扣费时操作。
/// </summary>
public class CapitalPower : TouhouAncientPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
}
