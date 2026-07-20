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
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 极奢形态 Power (Single)
/// - 打出非X费牌不再消耗能量，改为 1:10 消耗启动资金
/// - 启动资金不足时改为消耗金币
/// - 通过 TryModifyEnergyCostInCombat 将所有手牌费用置 0
/// - 在 AfterCardPlayed 中根据原耗能扣除资金/金币
/// </summary>
public class RichestFormPower : TouhouAncientPowerModel
{
    private class Data
    {
        public readonly Dictionary<CardType, int> CardTypePlayedThisTurn = new Dictionary<CardType, int>();
    }
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public int ExtraEnergy { private get; set; }


    protected override object InitInternalData()
    {
        return new Data();
    }
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.ForEnergy(this)
    ];

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        return base.AfterApplied(applier, cardSource);
    }

    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        if (!Owner.IsPlayer) return 0;
        if (player != base.Owner.Player)
            return amount;
        return amount + ExtraEnergy;
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

        modifiedCost = originalCost + (decimal)(GetInternalData<Data>().CardTypePlayedThisTurn.GetValueOrDefault(card.Type, 0) * Amount);
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