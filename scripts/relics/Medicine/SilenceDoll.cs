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
using MegaCrit.Sts2.Core.Models.Cards;
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
    private readonly List<CardModel> _trackedCards = new();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Retain)];

    public override Task BeforeCombatStart()
    {
        _trackedCards.Clear();
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner) return;
        if (player.Creature.CombatState == null) return;


        // 使用战斗历史记录获取上回合进入弃牌堆的牌（联机兼容）
        var available = CombatManager.Instance.History.Entries
            .OfType<CardDiscardedEntry>()
            .Where(e => e.HappenedLastPlayerTurn(player) && e.Card.Owner == base.Owner && e.Card is { HasBeenRemovedFromState: false, Pile: not null })
            .Select(e => e.Card)
            .Distinct()
            .ToList();

        if (available.Count == 0) return;

        Flash();

        var selected = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            available,
            base.Owner,
            new CardSelectorPrefs(base.SelectionScreenPrompt, 1, 1)
        )).FirstOrDefault();

        if (selected == null) return;
        _trackedCards.Add(selected);

        // 添加保留并放入手牌
        await CardPileCmd.Add(selected, PileType.Hand);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _trackedCards.Remove(cardPlay.Card);
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
        _trackedCards.Clear();
        return Task.CompletedTask;
    }
}
