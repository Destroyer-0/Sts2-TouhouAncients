using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 缄默魔偶：
/// 每回合开始时，选择一张上回合进入过弃牌堆的卡牌放入手牌，这张牌在打出前拥有保留。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class SilenceDoll : TouhouAncientRelics
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Retain)];

    private class Data
    {
        // 当前回合进入弃牌堆的牌
        public readonly List<CardModel> CurrentTurnDiscards = new();
        // 被标记为保留的牌
        public readonly List<CardModel> TrackedCards = new();
    }
    
    private Data _data = new();

    public override Task BeforeCombatStart()
    {
        _data.CurrentTurnDiscards.Clear();
        _data.TrackedCards.Clear();
        return Task.CompletedTask;
    }

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        if (card.Owner != base.Owner) return;

        // 卡牌进入弃牌堆时追踪（包括正常打出的牌）
        if (card.Pile?.Type == PileType.Discard)
        {
            _data.CurrentTurnDiscards.Add(card);
        }
    }
    
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner) return;

        // 上一回合进入弃牌堆的牌（当前还在弃牌堆中的）作为候选
        var available = _data.CurrentTurnDiscards
            .Where(c => c is { HasBeenRemovedFromState: false, Pile: not null } && c.Owner == base.Owner)
            .Distinct()
            .ToList();
        //
        //
        // var amount = CombatManager.Instance.History.Entries.OfType<CardDiscardedEntry>()
        //     .Count(e => e.HappenedLastPlayerTurn(player) && e.CardPlay.Card.Owner == card.Owner && e.CardPlay.Card.Type == card.Type);
        //
        _data.CurrentTurnDiscards.Clear();
        if (available.Count == 0) return;

        Flash();

        var selected = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            available,
            base.Owner,
            new CardSelectorPrefs(base.SelectionScreenPrompt, 1, 1)
        )).FirstOrDefault();

        if (selected == null) return;
        _data.TrackedCards.Add(selected);

        // 添加保留并放入手牌
        // selected.AddKeyword(CardKeyword.Retain);
        await CardPileCmd.Add(selected, PileType.Hand);
    }

    // public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    // {
    //     if (side != base.Owner.Creature.Side) return;
    //
    //     // 上一回合进入弃牌堆的牌（当前还在弃牌堆中的）作为候选
    //     var available = _data.CurrentTurnDiscards
    //         .Where(c => c is { HasBeenRemovedFromState: false, Pile: not null } && c.Owner == base.Owner)
    //         .Distinct()
    //         .ToList();
    //     _data.CurrentTurnDiscards.Clear();
    //
    //     if (available.Count == 0) return;
    //
    //     Flash();
    //
    //     var selected = (await CardSelectCmd.FromSimpleGrid(
    //         new BlockingPlayerChoiceContext(),
    //         available,
    //         base.Owner,
    //         new CardSelectorPrefs(base.SelectionScreenPrompt, 1, 1)
    //     )).FirstOrDefault();
    //
    //     if (selected == null) return;
    //     _data.TrackedCards.Add(selected);
    //
    //     // 添加保留并放入手牌
    //     // selected.AddKeyword(CardKeyword.Retain);
    //     await CardPileCmd.Add(selected, PileType.Hand);
    // }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var card = cardPlay.Card;
        if (!_data.TrackedCards.Contains(card)) return;
        _data.TrackedCards.Remove(card);
    }

    public override bool TryModifyKeywordsInCombat(CardModel card, ISet<CardKeyword> keywords)
    {
        if (card.Owner != base.Owner)
        {
            return false;
        }

        if (!_data.TrackedCards.Contains(card)) return false;
        return keywords.Add(CardKeyword.Retain);
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _data.CurrentTurnDiscards.Clear();
        _data.TrackedCards.Clear();
        return Task.CompletedTask;
    }


    protected override void AfterCloned()
    {
        base.AfterCloned();
        _data = new Data();
    }
}
