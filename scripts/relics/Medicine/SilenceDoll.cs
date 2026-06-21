using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
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
    // 当前回合进入弃牌堆的牌
    private readonly List<CardModel> _currentTurnDiscards = new();

    // 被缄默魔偶捞起的牌 → 是否原本就有保留
    private readonly List<CardModel> _trackedCards = new();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Retain)];

    public override Task BeforeCombatStart()
    {
        _currentTurnDiscards.Clear();
        _trackedCards.Clear();
        return Task.CompletedTask;
    }

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        if (card.Owner != base.Owner) return;

        // 卡牌进入弃牌堆时追踪（包括正常打出的牌）
        if (card.Pile?.Type == PileType.Discard)
        {
            _currentTurnDiscards.Add(card);
        }
    }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != base.Owner.Creature.Side) return;

        // 上一回合进入弃牌堆的牌（当前还在弃牌堆中的）作为候选
        var available = _currentTurnDiscards
            .Where(c => c is { HasBeenRemovedFromState: false, Pile: not null } && c.Owner == base.Owner)
            .Distinct()
            .ToList();
        _currentTurnDiscards.Clear();

        if (available.Count == 0) return;

        Flash();

        var selected = (await CardSelectCmd.FromSimpleGrid(
            new BlockingPlayerChoiceContext(),
            available,
            base.Owner,
            new CardSelectorPrefs(base.SelectionScreenPrompt, 1, 1)
        )).FirstOrDefault();

        if (selected == null) return;
        _trackedCards.Add(selected);

        // 添加保留并放入手牌
        // selected.AddKeyword(CardKeyword.Retain);
        await CardPileCmd.Add(selected, PileType.Hand);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var card = cardPlay.Card;
        if (!_trackedCards.Contains(card)) return;
        _trackedCards.Remove(card);
    }

    public override bool TryModifyKeywordsInCombat(CardModel card, ISet<CardKeyword> keywords)
    {
        if (card.Owner != base.Owner)
        {
            return false;
        }

        if (!_trackedCards.Contains(card)) return false;
        return keywords.Add(CardKeyword.Retain);
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _currentTurnDiscards.Clear();
        _trackedCards.Clear();
        return Task.CompletedTask;
    }
}
