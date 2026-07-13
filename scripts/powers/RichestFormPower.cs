using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
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
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public int ExtraEnergy { private get; set; }

    private readonly Dictionary<CardType, int> _cardTypePlayedThisTurn = new Dictionary<CardType, int>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.ForEnergy(this)
    ];

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        _cardTypePlayedThisTurn.Clear();
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
        if (cardPlay.Card.Owner.Creature != Owner)
        {
            return Task.CompletedTask;
        }

        if (!_cardTypePlayedThisTurn.TryGetValue(cardPlay.Card.Type, out var count))
        {
            _cardTypePlayedThisTurn[cardPlay.Card.Type] = 0;
        }

        _cardTypePlayedThisTurn[cardPlay.Card.Type] += 1;
        return base.AfterCardPlayed(choiceContext, cardPlay);
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card.Owner.Creature != base.Owner)
        {
            return false;
        }

        modifiedCost = originalCost + (decimal)(_cardTypePlayedThisTurn.GetValueOrDefault(card.Type, 0) * Amount);
        return true;
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (participants.Contains(base.Owner))
        {
            _cardTypePlayedThisTurn.Clear();
        }
    }
}