using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 疫病支票 (The Million Pound Note)
/// 0费，技能，保留。
/// 选择至多3张手牌，其耗能在下次打出前降为0。
/// 若一次性减少3点耗能以上，这张牌的费用增加1。
/// 在手牌中时，不能打出耗能低于此牌的卡牌。
/// </summary>
[Pool(typeof(EventCardPool))]
public class TheMillionPoundNote : TouhouAncientCards
{
    private const int energyCost = 0;
    private const CardType type = CardType.Curse;
    private const CardRarity rarity = CardRarity.Ancient;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;

    protected override bool ShouldGlowRedInternal => Owner.PlayerCombatState != null &&
                                                     Owner.PlayerCombatState.Hand.Cards
                                                         .Where(x => x != this && !x.EnergyCost.CostsX)
                                                         .All(x => x.EnergyCost.GetResolved() < EnergyCost.GetResolved());

    public override int MaxUpgradeLevel => 0;

    public TheMillionPoundNote() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(1),
        new CardsVar(3)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    private int _currentSfxId = -1;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放音效1：打出牌时

        var player = Owner;
        var hand = PileType.Hand.GetPile(player).Cards.Where(x => x.EnergyCost.GetResolved() > 0).ToList();
        hand.Remove(this);

        if (hand.Count == 0) return;

        if (LocalContext.IsMe(Owner))
        {
            _currentSfxId = NDebugAudioManager.Instance?.Play("MillionPounds1.mp3", 1.5f) ?? -1;
        }
        // 选择至多3张牌
        var maxSelect = Math.Min(3, hand.Count);
        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1, maxSelect);
        var selected = (await CardSelectCmd.FromHand(
            prefs: prefs,
            context: choiceContext,
            player: player,
            filter: null,
            source: this)).ToList();

        if (LocalContext.IsMe(Owner))
        {
            // 播放音效2：选择结束后（如果音效1还在播放则立即打断）
            if (_currentSfxId >= 0)
                NDebugAudioManager.Instance?.Stop(_currentSfxId, 0f);
            NDebugAudioManager.Instance?.Play("MillionPounds2.mp3", 1.5f);
        }

        if (selected.Count == 0) return;

        // 将选中牌的耗能降为0，并计算总减少量
        decimal totalReduction = 0;
        foreach (var card in selected)
        {
            var cost = card.EnergyCost.GetResolved();
            if (cost > 0)
            {
                totalReduction += cost;
                card.EnergyCost.AddUntilPlayed(-cost);
            }
        }

        // 若一次性减少3点耗能以上，自身费用+1
        if (totalReduction >= 3)
        {
            EnergyCost.AddThisCombat(1);
        }
    }

    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)
    {
        if (card.Owner != base.Owner) return true;
        if (base.Pile?.Type != PileType.Hand) return true;
        if (card == this) return true;
        if (autoPlayType != AutoPlayType.None) return true;
        if (card.EnergyCost.CostsX) return true;

        return card.EnergyCost.GetResolved() >= base.EnergyCost.GetResolved();
    }
}