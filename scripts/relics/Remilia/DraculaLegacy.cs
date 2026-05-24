using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.relics;


[HarmonyPatch]
public static class DraculaLegacyPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(RelicCmd), nameof(RelicCmd.Obtain), new[] { typeof(RelicModel), typeof(Player), typeof(int) })]
    private static void AfterObtainRelic(RelicModel relic, Player player)
    {
        // 玩家没有 DraculaLegacy → 不处理
        var draculaLegacy = player.GetRelic<DraculaLegacy>();
        if (draculaLegacy == null) return;

        // 获得的是 DraculaLegacy 自身 → 不惩罚
        if (relic == draculaLegacy) return;

        // 是本遗物生成的 6 个免费遗物之一 → 不惩罚
        if (draculaLegacy._spawnedRelicIds?.Contains(relic) == true) return;

        // 扣除 13 点生命
        _ = draculaLegacy.LoseHp();
    }
}

[Pool(typeof(SharedRelicPool))]
public class DraculaLegacy : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(13m, ValueProp.Unblockable | ValueProp.Unpowered)];

    public override bool HasUponPickupEffect => true;

    /// <summary>
    /// 本遗物生成的 6 个遗物的 Id，Patch 据此判断是否免伤。
    /// </summary>
    internal List<RelicModel>? _spawnedRelicIds;

    /// <summary>
    /// 拾起时，获得 6 个随机遗物（2 普通 / 2 罕见 / 2 稀有）。
    /// </summary>
    public override async Task AfterObtained()
    {
        _spawnedRelicIds = new List<RelicModel>(6);
        var rewards = GenerateRewards();
        await RewardsCmd.OfferCustom(base.Owner, rewards);
    }

    /// <summary>
    /// 预生成 6 个遗物并追踪其 Id，使 Patch 能识别哪些是本遗物免费提供的遗物。
    /// </summary>
    private List<Reward> GenerateRewards()
    {
        var player = base.Owner;
        var rewards = new List<Reward>(6);

        // 2 common, 2 uncommon, 2 rare — 预生成以追踪 Id
        var rarities = new[]
        {
            RelicRarity.Common, RelicRarity.Common,
            RelicRarity.Uncommon, RelicRarity.Uncommon,
            RelicRarity.Rare, RelicRarity.Rare
        };

        foreach (var rarity in rarities)
        {
            var relic = RelicFactory.PullNextRelicFromFront(player, rarity).ToMutable();
            _spawnedRelicIds!.Add(relic);
            rewards.Add(new RelicReward(relic, player));
        }

        return rewards;
    }

    public async Task LoseHp()
    {
        Flash();

        var damage = base.DynamicVars.Damage;

        // 非战斗状态：至少保留 1 点生命
        if (Owner.Creature.CombatState == null && damage.IntValue >= Owner.Creature.CurrentHp)
        {
            damage.UpgradeValueBy(Owner.Creature.CurrentHp - damage.IntValue - 1);
        }

        if (damage.IntValue > 0)
        {
            //await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), base.Owner.Creature, base.DynamicVars.Damage, null, null);
            await CreatureCmd.Damage(
                new ThrowingPlayerChoiceContext(),
                Owner.Creature,
                damage,
                null,
                null);
        }
    }
}
