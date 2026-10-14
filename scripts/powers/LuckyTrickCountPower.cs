using MegaCrit.Sts2.Core.Entities.Powers;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 幸运的代价：挂在玩家身上的「幸运」戏法打出计数，图标上的数字即该玩家的张数。
/// 由 InabaTewiMonster 在玩家打出「幸运」戏法时递增（第一次打出才创建，故开场不存在）。
/// 取 PowerType.None：Debuff 会被人工制品挡下，Buff 会被「全人类的绯想天」这类加层效果放大，都会让计数失真。
/// </summary>
public class LuckyTrickCountPower : TouhouAncientPowerModel
{
    public override PowerType Type => PowerType.None;

    public override PowerStackType StackType => PowerStackType.Counter;
}
