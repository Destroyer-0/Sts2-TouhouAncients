using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 沥血阴阳玉：每个回合开始时获得1能量。休息处恢复的生命值减少25点。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class BloodYinYangOrb : TouhouAncientRelics
{

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(1),
        new DynamicVar("HealPenalty", 25m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.ForEnergy(this)];
    
    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        if (player != base.Owner)
        {
            return amount;
        }
        return amount + (decimal)base.DynamicVars.Energy.IntValue;
    }
    
    /// <summary>
    /// 休息处恢复的生命值减少25点。
    /// </summary>
    public override decimal ModifyRestSiteHealAmount(Creature creature, decimal amount)
    {
        if (creature.Player != base.Owner && creature.PetOwner != base.Owner)
        {
            return amount;
        }
        Flash();
        return amount - base.DynamicVars["HealPenalty"].BaseValue;
    }
}
