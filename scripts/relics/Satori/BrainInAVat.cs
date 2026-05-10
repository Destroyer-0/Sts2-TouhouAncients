using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 缸中之脑：从25张本职业的牌中选择任意张牌，使其不会出现在后续的卡牌奖励与商店中。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class BrainInAVat : TouhouAncientRelics
{
    private static readonly int MaxChoices = 25;
    private static readonly int MaxChoicesCommon = 10;
    private static readonly int MaxChoicesUncommon = 10;

    /// <summary>被屏蔽的卡牌 ID 集合。
    [SavedProperty]
    private HashSet<string>? _blockedCardIds;
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Cards", MaxChoices)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            var title = new LocString("relics", base.Id.Entry + ".forgetTitle");
            if (!HasBlockedCard)
            {
                return [new HoverTip(title, new LocString("relics", base.Id.Entry + ".forgetNothing"))];
            }

            var description = new LocString("relics", base.Id.Entry + ".forget");
            var blockCardTextList = new List<string>();
            foreach (var cardId in _blockedCardIds)
            {
                blockCardTextList.Add(new LocString("cards", cardId + ".title").GetFormattedText());
            }

            description.Add("Cards", blockCardTextList);
            return [new HoverTip(title, description)];
        }
    }

    public override bool HasUponPickupEffect => true;

    private bool HasBlockedCard => _blockedCardIds is { Count: > 0 };

    public override async Task AfterObtained()
    {
        var player = base.Owner;
        // 获取本职业最多20张可用牌
        var allCards = player.Character.CardPool
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint).ToList()
            .UnstableShuffle(base.Owner.PlayerRng.Rewards);
        var common = allCards.Where(x => x.Rarity == CardRarity.Common).Take(MaxChoicesCommon);
        var uncommon = allCards.Where(x => x.Rarity == CardRarity.Uncommon).Take(MaxChoicesUncommon);
        var rare = allCards.Where(x => x.Rarity == CardRarity.Rare).Take(MaxChoices-MaxChoicesCommon-MaxChoicesUncommon);
        
        if (allCards.Count <= 0)
        {
            return;
        }
        
        // 从网格中选择要屏蔽的牌（任意张）
        var selected = await CardSelectCmd.FromSimpleGrid(
            context: new BlockingPlayerChoiceContext(),
            cardsIn: common.Concat(uncommon).Concat(rare).ToList(),
            player: player,
            prefs: new CardSelectorPrefs(base.SelectionScreenPrompt, 0, allCards.Count)
        );

        _blockedCardIds = selected.Select(c => c.Id.Entry).ToHashSet();
        if (_blockedCardIds.Count <= 0) return;

        Flash();
    }

    /// <summary>
    /// 在卡牌奖励生成前，排除被屏蔽的牌。
    /// 兼容 CardPools 和 CustomCardPool 两种模式。
    /// 安全处理：如果过滤后可选池为空，降级为不过滤，避免崩溃。
    /// </summary>
    public override CardCreationOptions ModifyCardRewardCreationOptions(Player player, CardCreationOptions options)
    {
        if (player != base.Owner) return options;
        if (!HasBlockedCard) return options;
        if (options.Flags.HasFlag(CardCreationFlags.NoCardPoolModifications)) return options;

        // GetPossibleCards 兼容 CardPools 和 CustomCardPool 两种模式
        var possibleCards = options.GetPossibleCards(player).ToList();
        var filtered = possibleCards.Where(c => !_blockedCardIds.Contains(c.Id.Entry)).ToList();

        // 全被屏蔽了 → 不过滤，避免空池崩溃
        if (filtered.Count <= 0) return options;

        return options.WithCustomPool(filtered);
    }
}
