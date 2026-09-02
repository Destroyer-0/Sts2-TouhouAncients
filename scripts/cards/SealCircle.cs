using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Rooms;
using TouhouAncients.Scripts.Afflictions;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 封魔阵：1 费状态牌，消耗。
/// 加入弃牌堆时自动从玩家的抽牌堆/手牌/弃牌堆中寻找一张
/// 「没有侵蚀、非状态/诅咒/任务」的牌，为其添加侵蚀「封印」，两两对应。
/// 被封印的牌无法被打出，直至这张封魔阵被消耗。
/// 若没有合法目标牌，描述为空白，本质变成一张只有消耗的状态牌。
/// </summary>
[Pool(typeof(StatusCardPool))]
public class SealCircle : TouhouAncientCards
{
    public override string? Author => "TOKIAME";
    
    private const int energyCost = 1;
    private const CardType type = CardType.Status;
    private const CardRarity rarity = CardRarity.Status;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;

    public override bool CanBeGeneratedByModifiers => false;
    public override bool CanBeGeneratedInCombat => false;

    /// <summary>本场战斗中被这张封魔阵封印的牌（战斗内数据，不跨战斗保存）。</summary>
    private CardModel? _sealedCard;

    /// <summary>是否已尝试过配对（防止 AfterCardEnteredCombat 重复触发）。</summary>
    private bool _pairingAttempted;

    protected override bool ShouldGlowRedInternal => _sealedCard is { Pile.Type: PileType.Hand };


    public SealCircle() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new StringVar("SealedText")
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    /// <summary>
    /// 战斗中已配对时，悬停显示被封印牌（若未配对则为空，不显示额外悬停）。
    /// </summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
        HoverTipFactory.FromAffliction<Sealed>(1).Concat(
        CombatManager.Instance.IsInProgress && _sealedCard != null
            ? [HoverTipFactory.FromCard(_sealedCard)]
            : []);
    
    
    /// <summary>
    /// 描述三态：
    /// - 非战斗：读取 descriptionIdle（解除对应卡牌的封印状态。）
    /// - 战斗中已配对：读取 descriptionSealed（解除{Card}的封印状态。），{Card} 为被封印牌名
    /// - 战斗中未配对（无合法目标）：空白。
    /// </summary>
    protected override void AddExtraArgsToDescription(LocString description)
    {
        string text;
        if (!CombatManager.Instance.IsInProgress || IsCanonical)
        {
            text = new LocString("cards", Id.Entry + ".descriptionIdle").GetFormattedText();
        }
        else if (_sealedCard != null)
        {
            var sealedDescription = new LocString("cards", Id.Entry + ".descriptionSealed");
            sealedDescription.Add("Card", _sealedCard.Title);
            text = sealedDescription.GetFormattedText();
        }
        else
        {
            text = new LocString("cards", Id.Entry + ".descriptionIdle").GetFormattedText();
        }

        ((StringVar)DynamicVars["SealedText"]).StringValue = text;
        description.Add("SealedText", text);
    }


    /// <summary>
    /// 进入战斗牌堆（加入弃牌堆）时自动配对。
    /// AfterCardEnteredCombat 在卡牌已进入目标牌堆后触发，此时可从各牌堆中寻找目标。
    /// </summary>
    public override async Task AfterCardEnteredCombat(CardModel card)
    {
        if (card != this) return;
        if (_pairingAttempted) return;
        if (card.Pile?.Type != PileType.Discard) return;

        _pairingAttempted = true;

        Player player = card.Owner;
        if (player.PlayerCombatState == null) return;

        // 候选范围：抽牌堆/手牌/弃牌堆，排除自己
        var candidates = PileType.Draw.GetPile(player).Cards
            .Concat(PileType.Hand.GetPile(player).Cards)
            .Concat(PileType.Discard.GetPile(player).Cards)
            .Where(c => c != this
                        && c.Affliction == null
                        && c.Type is not (CardType.Status or CardType.Curse or CardType.Quest))
            .ToList();

        if (candidates.Count == 0) return;

        var target = candidates.UnstableShuffle(player.RunState.Rng.CombatCardSelection).First();

        _sealedCard = target;
        var affliction = await CardCmd.Afflict<Sealed>(target, 1m);
        if (affliction != null)
        {
            affliction.SetSealCircle(this);
        }
    }

    /// <summary>
    /// 本场战斗结束时清空配对数据（战斗内引用不跨战斗保存）。
    /// </summary>
    public override Task AfterCombatEnd(CombatRoom room)
    {
        _sealedCard = null;
        _pairingAttempted = false;
        return Task.CompletedTask;
    }
}
