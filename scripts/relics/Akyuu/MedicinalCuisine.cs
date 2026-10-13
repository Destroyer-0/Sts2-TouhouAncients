using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using TouhouAncients.Scripts.potions;

namespace TouhouAncients.Scripts.relics.Akyuu;

/// <summary>
/// 调理药膳：拾起时，获得2瓶分装药膳。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class MedicinalCuisine : TouhouAncientRelics
{
    /// <summary>图标沿用已有的中文名素材，不改动任何图片文件。</summary>
    public override string DefaultFileName => "调理药膳";

    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Potions", 2m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPotion<PortionedMedicinalCuisinePotion>()];

    public override async Task AfterObtained()
    {
        Flash();

        // 药水栏不够时装不满，TryToProcure 满栏即失败
        for (var i = 0; i < base.DynamicVars["Potions"].IntValue; i++)
        {
            if (!base.Owner.HasOpenPotionSlots) break;
            await PotionCmd.TryToProcure<PortionedMedicinalCuisinePotion>(base.Owner);
        }
    }
}
