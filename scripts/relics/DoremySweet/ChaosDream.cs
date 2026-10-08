using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rewards;
using TouhouAncients.Scripts.cards;

namespace TouhouAncients.Scripts.relics.DoremySweet;

/// <summary>
/// 混沌之梦：拾起时，获得 2 件哆来咪·苏伊特的随机遗物（同涅奥骨骰发奖励面板），
/// 再将一张噩梦无终加入牌组。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class ChaosDream : TouhouAncientRelics
{
    /// <summary>哆来咪·苏伊特的全部遗物（不含本遗物，避免自指）。</summary>
    private static readonly RelicModel[] DoremyRelics =
    [
        ModelDb.Relic<SupremeDream>(),
        ModelDb.Relic<RevivalDream>(),
        ModelDb.Relic<BygoneDream>(),
        ModelDb.Relic<PioneerDream>(),
        ModelDb.Relic<RevelationDream>(),
        ModelDb.Relic<IridescentDream>(),
        ModelDb.Relic<MeltingWaxDream>(),
        ModelDb.Relic<BlazingFlameDream>(),
        ModelDb.Relic<WindPriestessDream>(),
        ModelDb.Relic<MeteorDream>(),
        ModelDb.Relic<ParadiseDream>(),
        ModelDb.Relic<SinisterPactDream>(),
        ModelDb.Relic<FlowingSplendorDream>(),
        ModelDb.Relic<BloodbathDream>(),
        ModelDb.Relic<BoundaryDream>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Relics", 2)];

    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<EndlessNightmare>();

    public override async Task AfterObtained()
    {
        var player = base.Owner;
        var owned = player.Relics.Select(relic => relic.Id).ToHashSet();

        var chosen = DoremyRelics
            .Where(relic => !owned.Contains(relic.Id)
                            && relic.IsAllowed(player.RunState)
                            && (relic is not TouhouAncientRelics ancient || ancient.CanAppear(player)))
            .ToList()
            .UnstableShuffle(player.PlayerRng.Rewards)
            .Take(DynamicVars["Relics"].IntValue)
            .ToList();

        if (chosen.Count > 0)
        {
            var rewards = new List<Reward>(chosen.Count);
            foreach (var relic in chosen)
            {
                var mutable = relic.ToMutable();
                // 虹光之梦要替换初始遗物与卡牌，正常流程在选项生成阶段 SetupForPlayer，这里补一次
                if (mutable is IridescentDream iridescent) iridescent.SetupForPlayer(player);
                rewards.Add(new RelicReward(mutable, player));
            }

            await new RewardsSet(player).WithCustomRewards(rewards).WithSkippingDisallowed().Offer();
        }

        var curse = player.RunState.CreateCard<EndlessNightmare>(player);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(curse, PileType.Deck), 2f);
        await Cmd.Wait(0.75f);
    }
}
