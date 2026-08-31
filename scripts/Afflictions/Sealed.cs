using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace TouhouAncients.Scripts.Afflictions;

/// <summary>
/// 封印：这张牌无法被打出，直至其对应的封魔阵被消耗。
/// 由封魔阵（SealCircle）在加入弃牌堆时自动施加，并互相记录引用。
/// </summary>
public sealed class Sealed : TouhouAncientAfflictionModel
{
    /// <summary>对应的封魔阵（战斗内引用，配对时设置）。</summary>
    private CardModel? _sealCircle;

    /// <summary>
    /// 设置对应的封魔阵。由封魔阵在施加此侵蚀时调用。
    /// </summary>
    public void SetSealCircle(CardModel sealCircle)
    {
        _sealCircle = sealCircle;
    }

    /// <summary>
    /// 悬停显示：无法打出关键字 + 对应的封魔阵卡牌（配对后）。
    /// </summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HasCard
            ? _sealCircle != null
                ? [HoverTipFactory.FromKeyword(CardKeyword.Unplayable), HoverTipFactory.FromCard(_sealCircle)]
                : [HoverTipFactory.FromKeyword(CardKeyword.Unplayable)]
            : [];

    /// <summary>
    /// 只能施加于非状态/诅咒/任务牌（与封魔阵的配对过滤一致，作为双重保障）。
    /// </summary>
    public override bool CanAfflictCardType(CardType cardType)
    {
        return cardType is not (CardType.Status or CardType.Curse or CardType.Quest);
    }

    public override bool TryModifyKeywordsInCombat(CardModel card, ISet<CardKeyword> keywords)
    {
        if (card != Card)
        {
            return false;
        }

        return keywords.Add(CardKeyword.Unplayable);
    }

    /// <summary>
    /// 对应的封魔阵进入消耗堆时，解除这张牌的封印。
    /// 若这张牌已被移出游戏（消耗/转换/移除），则跳过解除。
    /// </summary>
    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        if (_sealCircle == null) return Task.CompletedTask;
        if (card != _sealCircle) return Task.CompletedTask;
        if (card.Pile?.Type == PileType.Exhaust)
        {
            if (!Card.HasBeenRemovedFromState)
            {
                Card.ClearAfflictionInternal();
            }
        }

        return Task.CompletedTask;
    }
}
