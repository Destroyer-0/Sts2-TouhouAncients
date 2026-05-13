using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 幸运宝盒：拾起时获得3个药水栏位，用幸运补剂填满你的药水栏。
/// 参照 PotionBelt 实现。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class LuckyTreasureChest : TouhouAncientRelics
{
    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        Flash();

        // 获得 3 个药水栏位
        await PlayerCmd.GainMaxPotionCount(3, base.Owner);

        // 用幸运补剂填满所有空药水栏位
        for (int i = 0; i < base.Owner.MaxPotionCount; i++)
        {
            if (base.Owner.GetPotionAtSlotIndex(i) == null)
            {
                await PotionCmd.TryToProcure<LuckyTonic>(base.Owner);
            }
        }
    }
}
