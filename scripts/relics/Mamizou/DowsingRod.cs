using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using TouhouAncients.Scripts.cards;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 寻龙尺：你没有选择的卡牌奖励将会被记录。你可以在休息处花费50金币雇佣纳兹琳寻宝。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class DowsingRod : TouhouAncientRelics
{
    private const string _cardTitlesKey = "CardTitles";
    public const int GoldCost = 50;
    private const int ShiningTowerGuaranteeCount = 3;

    private List<SerializableCard> _storedCards = new();

    public override bool ShowCounter => true;

    public override int DisplayAmount => _storedCards.Count;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new GoldVar(GoldCost),
        new StringVar(_cardTitlesKey)
    ];

    [SavedProperty]
    public List<SerializableCard> StoredCards
    {
        get => _storedCards;
        private set
        {
            AssertMutable();
            _storedCards.Clear();
            _storedCards.AddRange(value);
            UpdateCardList();
        }
    }

    [SavedProperty]
    public int TreasureHuntCount { get; set; }

    protected override void AfterCloned()
    {
        base.AfterCloned();
        _storedCards = new List<SerializableCard>();
        TreasureHuntCount = 0;
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        new HoverTip(
            new LocString("rest_site_ui", "OPTION_TREASURE.name"),
            DescriptionForTip())
    ];

    private LocString DescriptionForTip()
    {
        var desc = new LocString("rest_site_ui", "OPTION_TREASURE.description");
        desc.Add("Gold", base.DynamicVars.Gold.IntValue);
        return desc;
    }

    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != base.Owner) return false;
        options.Add(new DowsingRodRestSiteOption(player, this));
        return true;
    }

    /// <summary>
    /// 记录被跳过的卡牌奖励到遗物存储中。
    /// </summary>
    internal void RecordSkippedCards(IEnumerable<CardModel> cards)
    {
        foreach (var card in cards)
        {
            var cloned = (CardModel)card.MutableClone();
            _storedCards.Add(cloned.ToSerializable());
        }
        UpdateCardList();
    }

    /// <summary>
    /// 将一张卡牌重新加入存储（用于光辉宝塔回收）。
    /// </summary>
    internal void AddTowerToStorage(CardModel card)
    {
        if (_storedCards.Any(x => x.Id == card.Id)) return;
        
        var cloned = (CardModel)card.MutableClone();
        _storedCards.Add(cloned.ToSerializable());
        UpdateCardList();
    }

    private void UpdateCardList()
    {
        base.Status = _storedCards.Count <= 0 ? RelicStatus.Disabled : RelicStatus.Normal;
        var stringVar = (StringVar)base.DynamicVars[_cardTitlesKey];
        if (_storedCards.Count == 0)
        {
            stringVar.StringValue = string.Empty;
        }
        else
        {
            stringVar.StringValue = string.Join('\n', _storedCards.Select(c => "- " + SaveUtil.CardOrDeprecated(c.Id!).Title));
        }

        InvokeDisplayAmountChanged();
    }

    /// <summary>
    /// 寻宝主逻辑：花费50金币，选择存储的卡牌，获得额外奖励。
    /// </summary>
    public async Task TreasureHunt()
    {
        Flash();

        // 花费50金币
        await PlayerCmd.LoseGold(DynamicVars.Gold.IntValue, base.Owner, GoldLossType.Spent);

        // 第一步：从存储的卡牌中选择任意张拿走
        if (_storedCards.Count > 0)
        {
            var availableCards = _storedCards
                .Select(s => new CardCreationResult(CardModel.FromSerializable(s)))
                .ToList();

            var selected = (await CardSelectCmd.FromSimpleGridForRewards(
                new BlockingPlayerChoiceContext(),
                availableCards,
                base.Owner,
                new CardSelectorPrefs(
                    RelicModel.L10NLookup(base.Id.Entry + ".selectionScreenPrompt"),
                    0,
                    availableCards.Count)
            )).ToList();

            foreach (var card in selected)
            {
                var match = _storedCards.FirstOrDefault(s => s.Id?.Entry == card.Id.Entry);
                if (match != null)
                {
                    _storedCards.Remove(match);
                    await CardPileCmd.Add(card, PileType.Deck);
                    CardCmd.Preview(card);
                }
            }

            UpdateCardList();
        }

        // 第二步：额外奖励
        TreasureHuntCount++;
        var extraRewards = new List<Reward>();

        // 100% 旧日遗物（TrashHeap 遗物池）
        RelicModel[] trashHeapRelics =
        [
            ModelDb.Relic<DarkstonePeriapt>(),
            ModelDb.Relic<DreamCatcher>(),
            ModelDb.Relic<HandDrill>(),
            ModelDb.Relic<MawBank>(),
            ModelDb.Relic<TheBoot>()
        ];
        var relic = base.Owner.PlayerRng.Rewards.NextItem(trashHeapRelics);
        if (relic != null)
        {
            extraRewards.Add(new RelicReward(relic.ToMutable(), base.Owner));
        }

        // 100% 旧日卡牌（TrashHeap 卡牌池）
        CardModel[] trashHeapCards =
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
        var cardTemplate = base.Owner.PlayerRng.Rewards.NextItem(trashHeapCards);
        if (cardTemplate != null)
        {
            var card = base.Owner.RunState.CreateCard(cardTemplate, base.Owner);
            extraRewards.Add(new SpecialCardReward(card, base.Owner));
        }

        // 光辉宝塔（33%，第三次必出）
        bool shouldOfferTower = false;
        bool hasTowerInDeck = PileType.Deck.GetPile(base.Owner).Cards.Any(c => c is ShiningTower);

        if (!hasTowerInDeck && TreasureHuntCount >= ShiningTowerGuaranteeCount)
        {
            shouldOfferTower = true;
        }
        else if (base.Owner.PlayerRng.Rewards.NextDouble() < 0.33)
        {
            shouldOfferTower = true;
        }

        if (shouldOfferTower)
        {
            var tower = base.Owner.RunState.CreateCard(ModelDb.Card<ShiningTower>(), base.Owner);
            extraRewards.Add(new SpecialCardReward(tower, base.Owner));
        }

        // 展示额外奖励（可跳过）
        await new RewardsSet(base.Owner).WithCustomRewards(extraRewards).Offer();
    }
}

/// <summary>
/// 寻宝休息处选项
/// </summary>
public class DowsingRodRestSiteOption : RestSiteOption
{
    private readonly DowsingRod _relic;

    public override string OptionId => "TREASURE";

    public override LocString Description
    {
        get
        {
            if (IsEnabled)
            {
                return new LocString("rest_site_ui", "OPTION_TREASURE.description");
            }

            return new LocString("rest_site_ui", "OPTION_TREASURE.descriptionDisabled");
        }
    }

    public override bool IsEnabled => _relic.Owner.Gold >= DowsingRod.GoldCost;

    public DowsingRodRestSiteOption(Player owner, DowsingRod relic) : base(owner)
    {
        _relic = relic;
    }

    public override async Task<bool> OnSelect()
    {
        await _relic.TreasureHunt();
        return true;
    }

    public override async Task DoLocalPostSelectVfx(CancellationToken ct = default)
    {
        NRestSiteRoom.Instance?.AddChildSafely(NRestSmokeVfx.Create());
        await Cmd.CustomScaledWait(1.5f, 2.5f, ignoreCombatEnd: false, ct);
    }
}

/// <summary>
/// Harmony 补丁：在卡牌奖励的 OnSkipped 时记录卡牌到寻龙尺。
/// </summary>
[HarmonyPatch(typeof(CardReward), nameof(CardReward.OnSkipped))]
public static class DowsingRodOnSkippedPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardReward __instance)
    {
        DowsingRodCardRewardHelper.RecordRemainingCards(__instance);
    }
}

/// <summary>
/// Harmony 补丁：在卡牌奖励的 OnSelect 完成后记录剩余卡牌到寻龙尺。
/// 使用 TargetMethod 模式访问 protected 方法。
/// </summary>
[HarmonyPatch]
public static class DowsingRodOnSelectPatch
{
    [HarmonyPostfix]
    private static void Postfix(CardReward __instance, ref Task<bool> __result)
    {
        var originalTask = __result;
        __result = ContinueWithRecordRemaining(originalTask, __instance);
    }

    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(CardReward), "OnSelect", Type.EmptyTypes);
    }

    private static async Task<bool> ContinueWithRecordRemaining(Task<bool> originalTask, CardReward instance)
    {
        var rewardComplete = await originalTask;
        if (rewardComplete)
        {
            DowsingRodCardRewardHelper.RecordRemainingCards(instance);
        }

        return rewardComplete;
    }
}

/// <summary>
/// 共享辅助方法。
/// </summary>
internal static class DowsingRodCardRewardHelper
{
    internal static void RecordRemainingCards(CardReward reward)
    {
        var dowsingRod = reward.Player.Relics.OfType<DowsingRod>().FirstOrDefault();
        if (dowsingRod == null) return;

        var cardsField = AccessTools.Field(typeof(CardReward), "_cards");
        if (cardsField?.GetValue(reward) is List<CardCreationResult> cards && cards.Count > 0)
        {
            dowsingRod.RecordSkippedCards(cards.Select(c => c.Card));
        }
    }
}
