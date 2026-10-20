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
[Pool(typeof(EventRelicPool))]
public class BrainInAVat : TouhouAncientRelics
{
    private static readonly int MaxChoices = 25;
    private static readonly int MaxChoicesCommon = 10;
    private static readonly int MaxChoicesUncommon = 10;

    /// <summary>被屏蔽的卡牌 ID 集合。
    /// BaseLib 的 [SavedProperty] 不支持 List&lt;string&gt;，故用制表符分隔的字符串存储。
    [SavedProperty] private string TouhouAncients_SavedBlockedCardIds { get; set; }
    private List<string>? _blockedCardIds;

    /// <summary>懒初始化：从 _savedBlockedCardIds 反序列化到 _blockedCardIds。</summary>
    private List<string> BlockedCardIds
    {
        get
        {
            if (_blockedCardIds == null)
            {
                _blockedCardIds = string.IsNullOrEmpty(TouhouAncients_SavedBlockedCardIds)
                    ? new List<string>()
                    : TouhouAncients_SavedBlockedCardIds.Split("\t").ToList();
            }
            return _blockedCardIds;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Cards", MaxChoices)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            if (!HasBlockedCard) return [];

            var title = new LocString("relics", base.Id.Entry + ".forgetTitle");
            var description = new LocString("relics", base.Id.Entry + ".forget");
            var blockCardTextList = new List<string>();
            foreach (var cardId in BlockedCardIds)
            {
                blockCardTextList.Add(new LocString("cards", cardId + ".title").GetFormattedText());
            }

            description.Add("Cards", blockCardTextList);
            return [new HoverTip(title, description)];
        }
    }

    public override bool HasUponPickupEffect => true;

    private bool HasBlockedCard => BlockedCardIds.Count > 0;

    /// <summary>该牌是否在本遗物的屏蔽列表内。供防晒霜判断要不要让路。</summary>
    public bool IsBlocked(string cardId) => BlockedCardIds.Contains(cardId);

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

        _blockedCardIds = selected.Select(c => c.Id.Entry).ToList();
        TouhouAncients_SavedBlockedCardIds = string.Join("\t", _blockedCardIds);
        if (_blockedCardIds.Count <= 0) return;

        Flash();
    }

    /// <summary>
    /// 在卡牌奖励生成前，排除被屏蔽的牌。
    /// 走 Late 通道并叠加过滤：Early 阶段还有大猎物猎人等会设过滤器，WithFilter 覆盖会互相顶掉。
    /// 安全处理：如果过滤后可选池为空，降级为不过滤，避免崩溃。
    /// </summary>
    public override CardCreationOptions ModifyCardRewardCreationOptionsLate(Player player, CardCreationOptions options)
    {
        if (player != base.Owner) return options;
        if (!HasBlockedCard) return options;
        if (options.Flags.HasFlag(CardCreationFlags.NoCardPoolModifications)) return options;

        // GetPossibleCards 兼容 CardPools 和 CustomCardPool 两种模式
        var blockedSet = new HashSet<string>(BlockedCardIds);
        var possibleCards = options.GetPossibleCards(player).ToList();
        var nonBlocked = possibleCards.Where(c => !blockedSet.Contains(c.Id.Entry)).ToList();

        // 未屏蔽的牌够 3 张就只加屏蔽过滤，不碰别的遗物已设的条件
        if (nonBlocked.Count >= 3)
        {
            return AndFilter(options, c => !blockedSet.Contains(c.Id.Entry));
        }

        // 不够 3 张时补回被屏蔽的牌兜底：本体按稀有度抽牌，空集合会直接抛异常
        var allowedIds = new HashSet<string>(nonBlocked.Select(c => c.Id.Entry));
        foreach (var c in possibleCards.StableShuffle(player.PlayerRng.Rewards))
        {
            if (allowedIds.Count >= 3) break;
            allowedIds.Add(c.Id.Entry);
        }

        return AndFilter(options, c => allowedIds.Contains(c.Id.Entry));
    }

    public override IEnumerable<CardModel> ModifyMerchantCardPool(Player player, IEnumerable<CardModel> options)
    {
        if (player != base.Owner) return options;
        if (!HasBlockedCard) return options;
        var blockedIds = BlockedCardIds;
        return options.Where(x => !blockedIds.Contains(x.Id.Entry));
    }
}
