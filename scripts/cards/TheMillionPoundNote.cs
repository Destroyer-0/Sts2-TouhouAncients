using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 疫病支票 (The Million Pound Note)
/// 0费，诅咒。
/// 给一张手牌添加重放，其耗能减少1。这张牌的耗能增加1。
/// 在你的下个回合开始时，将此卡返回你的手牌。
/// 如果这张牌在你的手中，你不能打出耗能低于这张牌的卡牌。
/// </summary>
[Pool(typeof(EventCardPool))]
public class TheMillionPoundNote : TouhouAncientCards
{
    public override string? Author => "黄瓜";

    private const int energyCost = 0;
    private const CardType type = CardType.Curse;
    private const CardRarity rarity = CardRarity.Curse;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;

    protected override bool ShouldGlowRedInternal => Owner.PlayerCombatState != null &&
                                                     Owner.PlayerCombatState.Hand.Cards
                                                         .Where(x => x != this && !x.EnergyCost.CostsX)
                                                         .All(x => x.EnergyCost.GetResolved() < EnergyCost.GetResolved());

    public override bool CanBeGeneratedByModifiers => false;
    public override bool CanBeGeneratedInCombat => false;

    public override bool UseAncientFrame => true;
    public override int MaxUpgradeLevel => 0;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new[]
    {
        HoverTipFactory.Static(StaticHoverTip.ReplayStatic)
    };

    public TheMillionPoundNote() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    private int _currentSfxId = -1;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放音效1：打出牌时
        if (LocalContext.IsMe(Owner))
        {
            _currentSfxId = NDebugAudioManager.Instance?.Play("MillionPounds1.mp3", 1.5f) ?? -1;
        }

        var hand = PileType.Hand.GetPile(Owner).Cards.Where(x => x != this).ToList();
        if (hand.Count == 0) return;

        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1, 1);
        var selected = (await CardSelectCmd.FromHand(
            prefs: prefs,
            context: choiceContext,
            player: Owner,
            filter: null,
            source: this)).ToList();

        if (selected.Count == 0) return;

        // 播放音效2：选择结束后（如果音效1还在播放则立即打断）
        if (LocalContext.IsMe(Owner))
        {
            if (_currentSfxId >= 0)
                NDebugAudioManager.Instance?.Stop(_currentSfxId, 0f);
            NDebugAudioManager.Instance?.Play("MillionPounds2.mp3", 1.5f);
        }

        var card = selected[0];
        card.BaseReplayCount++;
        card.EnergyCost.AddThisCombat(-1);
        EnergyCost.AddThisCombat(1);
    }

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        if (player != base.Owner) return;
        if (CombatManager.Instance.History.CardPlaysFinished.Any(e => e.HappenedLastPlayerTurn(base.Owner) && e.CardPlay.Card == this))
        {
            CardPile? pile = base.Pile;
            if (pile == null || pile.Type != PileType.Hand)
            {
                await CardPileCmd.Add(this, PileType.Hand);
            }
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