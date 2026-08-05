using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 菌类：奇幻蘑菇的共生标记能力。死亡时由蘑菇自身的死亡 Hook 向玩家弃牌堆加入孢子心灵。
/// </summary>
public class FungalPower : TouhouAncientPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
}
