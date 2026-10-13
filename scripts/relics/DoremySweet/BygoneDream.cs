using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rewards;

namespace TouhouAncients.Scripts.relics.DoremySweet;

/// <summary>
/// 往昔之梦：拾起时，以奖励面板发放一件被遗忘的旧日遗物与一张被遗忘的旧日卡牌（不可跳过）。
///
/// 「旧日遗物 / 旧日卡牌」沿用原版事件「垃圾堆」（TrashHeap）的固定池，
/// 与本 Mod「纳兹琳灵摆」的寻宝奖励使用同一套数据。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class BygoneDream : TouhouAncientRelics
{
    /// <summary>旧日遗物池（原版 TrashHeap 事件）。</summary>
    private static readonly RelicModel[] TrashHeapRelics =
    [
        ModelDb.Relic<DarkstonePeriapt>(),
        ModelDb.Relic<DreamCatcher>(),
        ModelDb.Relic<HandDrill>(),
        ModelDb.Relic<MawBank>(),
        ModelDb.Relic<TheBoot>()
    ];

    /// <summary>旧日卡牌池（原版 TrashHeap 事件）。</summary>
    private static readonly CardModel[] TrashHeapCards =
    [
        ModelDb.Card<Caltrops>(),
        ModelDb.Card<Clash>(),
        ModelDb.Card<Distraction>(),
        ModelDb.Card<DualWield>(),
        ModelDb.Card<Entrench>(),
        ModelDb.Card<HelloWorld>(),
        ModelDb.Card<Outmaneuver>(),
        ModelDb.Card<Rebound>(),
        ModelDb.Card<RipAndTear>(),
        ModelDb.Card<Stack>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        var player = base.Owner;
        if (player?.Creature == null) return;

        var rng = player.PlayerRng.Rewards;

        Flash();

        var rewards = new List<Reward>(2);

        // 一件旧日遗物
        var relic = rng.NextItem(TrashHeapRelics);
        if (relic != null)
        {
            rewards.Add(new RelicReward(relic.ToMutable(), player));
        }

        // 一张旧日卡牌
        var template = rng.NextItem(TrashHeapCards);
        if (template != null)
        {
            rewards.Add(new SpecialCardReward(player.RunState.CreateCard(template, player), player));
        }

        if (rewards.Count > 0)
        {
            // 不可跳过：语义等同「拾起时即获得」，玩家无法把奖励丢掉
            await new RewardsSet(player).WithCustomRewards(rewards).WithSkippingDisallowed().Offer();
        }
    }
}
