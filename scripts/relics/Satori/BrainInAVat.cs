using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 缸中之脑：从20张本职业的牌中选择任意张牌，使其不会出现在后续的卡牌奖励与商店中。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class BrainInAVat : TouhouAncientRelics
{
    private static readonly int MaxChoices = 20;

    /// <summary>被屏蔽的卡牌 ID 集合。
    [SavedProperty]
    private HashSet<string>? _blockedCardIds;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Cards", MaxChoices)];

    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        var player = base.Owner;
        var pool = player.Character.CardPool;

        // 获取本职业最多20张可用牌
        var allCards = pool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Take(MaxChoices)
            .ToList();
        if (allCards.Count <= 0) return;

        // 从网格中选择要屏蔽的牌（任意张）
        var selected = await CardSelectCmd.FromSimpleGrid(
            context: new BlockingPlayerChoiceContext(),
            cardsIn: allCards.Select(c => player.RunState.CloneCard(c)).ToList(),
            player: player,
            prefs: new CardSelectorPrefs(base.SelectionScreenPrompt, 0, allCards.Count)
        );

        _blockedCardIds = selected.Select(c => c.Id.Entry).ToHashSet();
        if (_blockedCardIds.Count <= 0) return;

        Flash();
    }

    /// <summary>
    /// 在卡牌奖励生成前，通过 CardPoolFilter 排除被屏蔽的牌。
    /// 调用时机：CardFactory.CreateForReward 每张牌生成前 → 不会减少奖励数量。
    /// 安全处理：如果过滤后可选池为空，降级为不过滤，避免崩溃。
    /// </summary>
    public override CardCreationOptions ModifyCardRewardCreationOptions(Player player, CardCreationOptions options)
    {
        if (player != base.Owner) return options;
        if (_blockedCardIds == null || _blockedCardIds.Count <= 0) return options;
        if (options.Flags.HasFlag(CardCreationFlags.NoCardPoolModifications)) return options;
        if (options.CustomCardPool != null) return options;

        var originalFilter = options.CardPoolFilter;

        // 先检查过滤后是否还有牌 → 避免整池被清空导致崩溃
        var allCards = options.CardPools
            .SelectMany(p => p.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint))
            .Where(c => (originalFilter == null || originalFilter(c)));
        var remaining = allCards.Count(c => !_blockedCardIds.Contains(c.Id.Entry));
        if (remaining <= 0) return options; // 全被屏蔽了 → 不过滤，让玩家拿到被屏蔽的牌

        Func<CardModel, bool> combinedFilter = c =>
            !_blockedCardIds.Contains(c.Id.Entry)
            && (originalFilter == null || originalFilter(c));

        return options.WithCardPools(options.CardPools, combinedFilter);
    }
}
