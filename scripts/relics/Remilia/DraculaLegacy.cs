using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace TouhouAncients.Scripts.relics;

[Pool(typeof(SharedRelicPool))]
public class DraculaLegacy : TouhouAncientRelics
{
    private const string _relicsKey = "Relics";

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar(_relicsKey, 6m)];

    public override bool HasUponPickupEffect => true;

    /// <summary>
    /// 防止 TryModifyRewardsLate 卡掉自己生成的遗物奖励。
    /// AfterObtained → OfferCustom → GenerateWithoutOffering → TryModifyRewardsLate 也会被触发。
    /// </summary>
    private bool _isOfferingOwnRewards;

    /// <summary>
    /// 拾起时，获得6个随机遗物。
    /// </summary>
    public override async Task AfterObtained()
    {
        _isOfferingOwnRewards = true;
        await RewardsCmd.OfferCustom(base.Owner, GenerateRewards());
        _isOfferingOwnRewards = false;
    }

    /// <summary>
    /// 移除后续奖励中所有遗物奖励（不能再获得遗物）。
    /// </summary>
    public override bool TryModifyRewardsLate(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != base.Owner) return false;
        if (_isOfferingOwnRewards) return false; // 不卡自己生成的遗物

        var relicRewards = rewards.OfType<RelicReward>().ToList();
        if (relicRewards.Count <= 0) return false;

        foreach (var r in relicRewards)
        {
            rewards.Remove(r);
        }
        return true;
    }

    private List<Reward> GenerateRewards()
    {
        var player = base.Owner;
        var rewards = new List<Reward>(6);
        CollectionsMarshal.SetCount(rewards, 6);
        var span = CollectionsMarshal.AsSpan(rewards);
        
        // 2 common, 2 uncommon, 2 rare
        span[0] = new RelicReward(RelicRarity.Common, player);
        span[1] = new RelicReward(RelicRarity.Common, player);
        span[2] = new RelicReward(RelicRarity.Uncommon, player);
        span[3] = new RelicReward(RelicRarity.Uncommon, player);
        span[4] = new RelicReward(RelicRarity.Rare, player);
        span[5] = new RelicReward(RelicRarity.Rare, player);

        return rewards;
    }
}
