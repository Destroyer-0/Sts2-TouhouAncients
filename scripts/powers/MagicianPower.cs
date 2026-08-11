using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 普通的魔法使：魔理沙的被动能力。
/// 使用极限火花·蓄力时，场上的每个蘑菇为魔理沙提供活力。
/// </summary>
public class MagicianPower : TouhouAncientPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<VigorPower>()];
}
