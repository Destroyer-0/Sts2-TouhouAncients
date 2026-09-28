using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rewards;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 近似两级的营帐：拾起时，先由玩家指定降级一张牌，再随机降级一张牌；
/// 然后以奖励列表的方式给出微型帐篷与随机两件非先古的休息处遗物。
/// 「非先古的休息处遗物」硬编码为：壶铃、皇家枕头、铲子、小邮箱、捕梦网。
/// 这些遗物里玩家已经拥有的不会出现在奖励列表中；缺少的不足两件时本遗物不作为先古选项出现。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class Jinsiliangjideyingzhang : TouhouAncientRelics
{
    /// <summary>奖励列表里给出的非先古休息处遗物数量（微型帐篷之外的随机部分）。</summary>
    private const int RandomRestRelicCount = 2;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Relics", RandomRestRelicCount)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromRelic<MiniatureTent>();

    public override bool HasUponPickupEffect => true;

    /// <summary>硬编码的非先古休息处遗物池：壶铃、皇家枕头、铲子、小邮箱、捕梦网（微型帐篷另行固定给出）。</summary>
    private static IReadOnlyList<RelicModel> RestRelicPool =>
    [
        ModelDb.Relic<Girya>(),
        ModelDb.Relic<RegalPillow>(),
        ModelDb.Relic<Shovel>(),
        ModelDb.Relic<TinyMailbox>(),
        ModelDb.Relic<DreamCatcher>(),
    ];

    /// <summary>取玩家尚未拥有的非先古休息处遗物（用 ModelId 比对，与遗物实例无关）。</summary>
    private static List<RelicModel> GetMissingRestRelics(Player player) =>
        RestRelicPool.Where(relic => player.Relics.All(owned => owned.Id != relic.Id)).ToList();

    /// <summary>选项条件：缺少的非先古休息处遗物少于两件时不出现。</summary>
    public override bool CanAppear(Player? player)
    {
        if (player == null) return false;
        return GetMissingRestRelics(player).Count >= RandomRestRelicCount;
    }

    public override async Task AfterObtained()
    {
        var player = base.Owner;

        // 一、玩家指定降级一张牌。只有已升级的牌可以被降级，牌组里没有时跳过这一步。
        var selected = (await CardSelectCmd.FromDeckGeneric(
            player,
            new CardSelectorPrefs(
                RelicModel.L10NLookup(base.Id.Entry + ".selectionScreenPrompt"),
                1
            ),
            card => card.IsUpgraded)).FirstOrDefault();
        if (selected != null)
        {
            CardCmd.Downgrade(selected);
            CardCmd.Preview(selected, 1.2f, CardPreviewStyle.MessyLayout);
            await Cmd.CustomScaledWait(0.3f, 0.5f);
        }

        // 二、随机再降级一张牌。上一步刚被降级的牌已不再是已升级状态，不会被重复选中。
        var downgradableCards = player.Deck.Cards.Where(card => card.IsUpgraded).ToList();
        if (downgradableCards.Count > 0)
        {
            var randomCard = downgradableCards.UnstableShuffle(player.PlayerRng.Rewards).First();
            CardCmd.Downgrade(randomCard);
            CardCmd.Preview(randomCard, 1.2f, CardPreviewStyle.MessyLayout);
            await Cmd.CustomScaledWait(0.3f, 0.5f);
        }

        // 三、奖励列表：固定给出微型帐篷，再从玩家还没有的非先古休息处遗物里随机取两件。
        var rewards = new List<Reward>
        {
            new RelicReward(ModelDb.Relic<MiniatureTent>().ToMutable(), player)
        };

        var candidates = GetMissingRestRelics(player);
        foreach (var relic in candidates.UnstableShuffle(player.PlayerRng.Rewards).Take(RandomRestRelicCount))
        {
            rewards.Add(new RelicReward(relic.ToMutable(), player));
        }

        await RewardsCmd.OfferCustom(player, rewards);
    }
}
