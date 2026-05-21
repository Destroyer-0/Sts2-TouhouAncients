using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Afflictions;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 饥饿背包：每场战斗开始时额外抽4张牌，
/// 额外抽到的牌在本回合获得吞噬（Devoured）。
/// 每个回合开始时，额外抽牌数减少1。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class HungryBackpack : TouhouAncientRelics
{
    private int _currentExtraDraw;
    private readonly HashSet<CardModel> _affectedCards = new();

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(4),
        new DynamicVar("ReduceCardDraw", 1m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromAffliction<Devoured>(1);

    public override bool ShowCounter => DisplayAmount > -1;

    public override int DisplayAmount =>
        !CombatManager.Instance.IsInProgress ? -1 : IsCanonical ? -1 : _currentExtraDraw;

    /// <summary>
    /// 每场战斗开始时重置额外抽牌数。
    /// </summary>
    public override Task BeforeCombatStart()
    {
        _currentExtraDraw = base.DynamicVars.Cards.IntValue;
        base.Status = RelicStatus.Active;
        InvokeDisplayAmountChanged();
        _affectedCards.Clear();
        return Task.CompletedTask;
    }

    public override decimal ModifyHandDraw(Player player, decimal count)
    {
        if (player != base.Owner)
        {
            return count;
        }

        if (_currentExtraDraw <= 0)
        {
            return count;
        }

        return count + _currentExtraDraw;
    }
    
    
    public override async Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature?.CombatState == null) return;
        if (_currentExtraDraw <= 0) return;
        var candidates =
            CombatManager.Instance.History.Entries
                .OfType<CardDrawnEntry>()
                .Where(e => e.HappenedThisTurn(player.Creature.CombatState) && e.Card.Owner == player)
                .Select(x => x.Card)
                .Where(c => c.Affliction == null)
                .ToList();

        if (candidates.Count <= 0) return;

        Flash();
        
        var selected = candidates
            .StableShuffle(player.RunState.Rng.CombatCardSelection)
            .Take(Math.Min(candidates.Count, _currentExtraDraw))
            .ToList();

        foreach (var card in selected)
        {
            if (card.Affliction != null) continue;
            var devoured = await CardCmd.Afflict<Devoured>(card, 1m);
            if (devoured != null && !card.Keywords.Contains(CardKeyword.Exhaust))
            {
                CardCmd.ApplyKeyword(card, CardKeyword.Exhaust);
                devoured.AppliedExhaust = true;
            }
            _affectedCards.Add(card);
        }

        _currentExtraDraw = Math.Max(0, _currentExtraDraw - base.DynamicVars["ReduceCardDraw"].IntValue);
        InvokeDisplayAmountChanged();
        if (_currentExtraDraw <= 0) base.Status = RelicStatus.Disabled;
    }
    
    /// <summary>
    /// 回合结束时清除本回合附加的吞噬效果。
    /// </summary>
    public override Task AfterTurnEndLate(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == CombatSide.Player)
        {
            foreach (var card in _affectedCards)
            {
                if (!card.IsInCombat) continue;
                if (card.Affliction is not Devoured) continue;
                if (((Devoured)card.Affliction).AppliedExhaust)
                {
                    CardCmd.RemoveKeyword(card, CardKeyword.Exhaust);
                }

                CardCmd.ClearAffliction(card);
            }

            _affectedCards.Clear();
        }
        return Task.CompletedTask;
    }
    
    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (!(room is CombatRoom))
        {
            return Task.CompletedTask;
        }
        base.Status = RelicStatus.Normal;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }
}
