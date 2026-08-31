using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 极奢形态 
/// </summary>
public class RichestFormPower : TouhouAncientPowerModel
{
    private class Data
    {
        public readonly Dictionary<CardType, int> CardTypePlayedThisTurn = new Dictionary<CardType, int>();
    }
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;


    public int ExtraCost
    {
        get => DynamicVars["ExtraCost"].IntValue;
        set => DynamicVars["ExtraCost"].BaseValue = value;
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("ExtraCost", 0m)
    ];

    protected override object InitInternalData()
    {
        return new Data();
    }
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.ForEnergy(this)
    ];

    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        if (player != base.Owner.Player)
            return amount;
        return amount + Amount;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner.Player)
        {
            return Task.CompletedTask;
        }

        if (!GetInternalData<Data>().CardTypePlayedThisTurn.TryGetValue(cardPlay.Card.Type, out var count))
        {
            GetInternalData<Data>().CardTypePlayedThisTurn[cardPlay.Card.Type] = 0;
        }

        GetInternalData<Data>().CardTypePlayedThisTurn[cardPlay.Card.Type] += 1;
        return Task.CompletedTask;
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card.Owner != base.Owner.Player)
        {
            return false;
        }

        modifiedCost = originalCost + (decimal)(GetInternalData<Data>().CardTypePlayedThisTurn.GetValueOrDefault(card.Type, 0) * ExtraCost);
        return true;
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (participants.Contains(base.Owner))
        {
            GetInternalData<Data>().CardTypePlayedThisTurn.Clear();
        }
    }
}