using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.relics;

[Pool(typeof(SharedRelicPool))]
public class DraculaLegacy : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(13m, ValueProp.Unblockable | ValueProp.Unpowered)];

    public override bool HasUponPickupEffect => true;

    /// <summary>本遗物生成的 6 个遗物奖励引用。用于判断后续获得遗物是否来自本遗物。</summary>
    private List<Reward>? _ourRewards;

    /// <summary>
    /// 拾起时，获得6个随机遗物。
    /// </summary>
    public override async Task AfterObtained()
    {
        _ourRewards = GenerateRewards();
        await RewardsCmd.OfferCustom(base.Owner, _ourRewards);
    }

    /// <summary>
    /// 不以此方式获得遗物时，失去13点生命。
    /// </summary>
    public override async Task AfterRewardTaken(Player player, Reward reward)
    {
        if (player != base.Owner) return;

        // 是本遗物生成的奖励 → 不惩罚
        if (reward is RelicReward relicReward && (_ourRewards != null && _ourRewards.Contains(relicReward)))
        {
            if (_ourRewards == null)
            {
                GD.PrintErr($"捡起自身生成的奖励、_ourRewards为空？！");
            }
            else
            {
                GD.PrintErr($"捡起自身生成的奖励{_ourRewards.Count}");
            }
            return;
        }

        // 不是本遗物获得的遗物 → 失去13点生命
        if (reward is RelicReward)
        {
            GD.PrintErr("捡起其他遗物");
            Flash();
            await CreatureCmd.Damage(
                new ThrowingPlayerChoiceContext(),
                player.Creature,
                base.DynamicVars.Damage,
                null,
                null);
        }
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
