using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using TouhouAncients.Scripts.potions;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 鲵吞亭特调：拾起时，获得{PotionSlots}个药水栏位。你获得的药水变成超ZUN啤酒。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class NidomiteiSpecialBlend : TouhouAncientRelics
{
    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("PotionSlots", 2m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPotion<SuperZunBeerPotion>(),
    ];

    public override async Task AfterObtained()
    {
        Flash();

        var slotCount = base.DynamicVars["PotionSlots"].IntValue;
        await PlayerCmd.GainMaxPotionCount(slotCount, base.Owner);
    }
}
