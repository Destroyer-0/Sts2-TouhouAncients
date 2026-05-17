using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 折叠伞：战斗开始时，获得5层倒映（ReflectPower）。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class FoldingUmbrella : TouhouAncientRelics
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<ReflectPower>()];

    public override async Task BeforeCombatStart()
    {
        Flash();
        await PowerCmd.Apply<ReflectPower>(base.Owner.Creature, 5m, base.Owner.Creature, null);
    }
}
