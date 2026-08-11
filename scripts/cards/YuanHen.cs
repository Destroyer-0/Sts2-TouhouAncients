using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 遗恨 — 诅咒牌
/// 1费，保留，永恒。不可升级。
/// 如果这张牌在你的手中，你从非手牌区打出牌时，将一张毒素（Toxic）加入你的手牌。
/// </summary>
[Pool(typeof(CurseCardPool))]
public class YuanHen : TouhouAncientCards
{
    public override bool CanBeGeneratedByModifiers => false;
    public override bool CanBeGeneratedInCombat => false;

    private const int energyCost = 1;
    private const CardType type = CardType.Curse;
    private const CardRarity rarity = CardRarity.Curse;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Retain,
        CardKeyword.Eternal
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromCardWithCardHoverTips<Toxic>();

    public override int MaxUpgradeLevel => -1;

    public YuanHen() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    /// <summary>
    /// 记录最近一次自动打出牌在打出前的堆类型。
    /// BeforeCardAutoPlayed 在牌被移动前触发，此时 Pile 仍为原堆。
    /// </summary>
    private PileType? _autoPlayedFromPile;

    public override Task BeforeCardAutoPlayed(CardModel card, Creature? target, AutoPlayType type)
    {
        if (card.Owner != base.Owner) return Task.CompletedTask;
        if (card == this) return Task.CompletedTask;
        _autoPlayedFromPile = card.Pile?.Type;
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != base.Owner) return;
        if (cardPlay.Card == this) return;
        // 只有自动打出（从非手牌区打出的牌必然经过 AutoPlay）才触发
        if (!cardPlay.IsAutoPlay) return;

        var fromPile = _autoPlayedFromPile;
        _autoPlayedFromPile = null;
        // 必须是从非手牌区打出
        if (fromPile == null || fromPile == PileType.Hand) return;
        // 遗恨必须在手牌中
        if (base.Pile?.Type != PileType.Hand) return;

        var player = base.Owner;
        var combatState = player.Creature.CombatState;
        if (combatState == null) return;

        // 将一张毒素加入手牌
        var toxic = combatState.CreateCard(ModelDb.Card<Toxic>(), player);
        await CardPileCmd.AddGeneratedCardToCombat(toxic, PileType.Hand, player);
    }
}
