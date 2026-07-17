using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using TouhouAncients.Scripts.cards;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 百万英镑：在每场战斗开始时，将一张疫病支票加入手牌。
/// 手牌有疫病支票时，每回合第一张牌免费，并将疫病支票费用提升至打出牌的原费用。
/// 疫病支票：诅咒，保留。在手牌中时，不能打出费用低于此牌的牌。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class MillionPounds : TouhouAncientRelics
{
    private int _cardsPlayedThisTurn;
    private readonly Dictionary<CardModel, decimal> _originalCosts = new();

    private int PlagueCheckCost = 0;
    private bool HasPlagueCheckInHand =>
        CombatManager.Instance.IsInProgress && Owner.PlayerCombatState != null && Owner.PlayerCombatState.Hand.Cards.Any(c => c is TheMillionPoundNote);

    public override bool ShowCounter => CombatManager.Instance.IsInProgress;
    public override int DisplayAmount => ShowCounter ? PlagueCheckCost : 0;

    
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromCard<TheMillionPoundNote>() };

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext,
        ICombatState combatState)
    {
        if (player != base.Owner) return;
        if (player.Creature.CombatState?.RoundNumber != 1) return;

        Flash();
        var note = player.Creature.CombatState.CreateCard<TheMillionPoundNote>(player);
        await CardPileCmd.AddGeneratedCardsToCombat([note], PileType.Hand, creator: base.Owner);
        PlagueCheckCost = 0;
        InvokeDisplayAmountChanged();
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        PlagueCheckCost = 0;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public override bool TryModifyEnergyCostInCombatLate(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card.Owner != base.Owner) return false;
        if (!HasPlagueCheckInHand) return false;
        if (card is TheMillionPoundNote) return false;

        if (_cardsPlayedThisTurn < 1)
        {
            _originalCosts.TryAdd(card, originalCost);
            modifiedCost = 0m;
            return true;
        }

        return false;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != base.Owner) return;
        if (Owner.PlayerCombatState == null) return;
        _cardsPlayedThisTurn++;

        // 本回合第一张牌且手牌有疫病支票（已由 TryModifyEnergyCostInCombatLate 免费）
        if (_cardsPlayedThisTurn == 1 && HasPlagueCheckInHand)
        {
            int originalCost = _originalCosts.TryGetValue(cardPlay.Card, out var cost) ? (int)cost : 0;
            _originalCosts.Remove(cardPlay.Card);
            int newCost = Math.Max(PlagueCheckCost, originalCost);
            PlagueCheckCost = newCost;

            foreach (var card in Owner.PlayerCombatState.AllCards)
            {
                if (card.HasBeenRemovedFromState)
                {
                    continue;
                }

                if (card is TheMillionPoundNote)
                {
                    card.EnergyCost.SetThisCombat(Math.Max(card.EnergyCost.GetResolved(), originalCost));
                }
            }

            InvokeDisplayAmountChanged();
        }
    }

    public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side == base.Owner.Creature.Side)
        {
            _cardsPlayedThisTurn = 0;
            _originalCosts.Clear();
        }
        return Task.CompletedTask;
    }
}