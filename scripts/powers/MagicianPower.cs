using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 普通的魔法使：魔理沙的被动能力。
/// 使用极限火花·蓄力时，场上的每个蘑菇为魔理沙恢复生命并提供活力。
/// 层数（Amount）为每个蘑菇提供的活力值；Heal 为每个蘑菇提供的恢复值（按幕数值）。
/// </summary>
public class MagicianPower : TouhouAncientPowerModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Heal", 7m)
    ];

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<VigorPower>()];

    /// <summary>
    /// 设置每个蘑菇提供的恢复值（按幕数值，运行时在 <see cref="AfterAddedToRoom"/> 中设置）。
    /// </summary>
    public void SetHeal(decimal heal) => base.DynamicVars["Heal"].BaseValue = CombatState.Players.Count == 1 ? heal : GetScaledAmountForMultiplayer(CombatState, Applier, heal, Owner, null);

    public void TryFlash() => Flash();
}
