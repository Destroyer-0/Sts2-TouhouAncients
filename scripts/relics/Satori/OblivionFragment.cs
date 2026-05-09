using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 遗忘残片：获得随机三个涅奥的初始遗物。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class OblivionFragment : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Relics", 3)];

    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        var state = base.Owner.RunState;

        // 获取所有涅奥初始遗物，排除自身
        var neowRelics = ModelDb.Event<Neow>().AllPossibleOptions
            .Where(o => o.Relic != null && o.Relic.IsAllowed(state) && !(o.Relic is OblivionFragment))
            .Select(o => o.Relic).OfType<RelicModel>()
            .ToList();

        // 随机取3个
        var chosen = neowRelics.UnstableShuffle(base.Owner.PlayerRng.Rewards).Take(DynamicVars["Relics"].IntValue).ToList();

        // 生成奖励
        var rewards = chosen.Select(r => new RelicReward(r, base.Owner)).ToList<Reward>();
        await new RewardsSet(base.Owner).WithCustomRewards(rewards).Offer();
        base.Status = RelicStatus.Disabled;
    }
}
