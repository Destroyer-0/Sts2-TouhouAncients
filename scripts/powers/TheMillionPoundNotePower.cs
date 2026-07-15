using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using TouhouAncients.Scripts.cards;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 名流 (The Million Pound Note Power — Celebrity)
/// - 每回合前 {Amount} 张牌免费打出
/// - 每免费打出一张牌时，将 1 张债务加入弃牌堆
/// </summary>
public class TheMillionPoundNotePower : TouhouAncientPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private int _cardsPlayedThisTurn;
    private bool _currentCardIsFree;

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        _currentCardIsFree = false;
        if (card.Owner.Creature != base.Owner) return false;

        if (_cardsPlayedThisTurn < Amount)
        {
            modifiedCost = 0m;
            _currentCardIsFree = true;
            return true;
        }

        return false;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature != base.Owner) return;
        _cardsPlayedThisTurn++;

        if (_currentCardIsFree)
        {
            _currentCardIsFree = false;
            await CardPileCmd.AddToCombatAndPreview<Debt>(base.Owner, PileType.Discard, 1, creator: cardPlay.Card.Owner, CardPilePosition.Random);
        }
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (participants.Contains(base.Owner))
        {
            _cardsPlayedThisTurn = 0;
            _currentCardIsFree = false;
        }
    }
}
