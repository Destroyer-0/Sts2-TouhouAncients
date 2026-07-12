using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 死黑的冠冕 — 每回合你打出的前2张牌造成的伤害翻倍。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class DeathBlackCrown : TouhouAncientRelics
{
    private int _cardsPlayedThisTurn;

    public override bool ShowCounter => CombatManager.Instance.IsInProgress;

    public override int DisplayAmount => _cardsPlayedThisTurn;

    private int CardsPlayedThisTurn
    {
        get => _cardsPlayedThisTurn;
        set
        {
            AssertMutable();
            _cardsPlayedThisTurn = value;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("CardThreshold", 2)
    ];

    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side == base.Owner.Creature.Side)
        {
            CardsPlayedThisTurn = 0;
            RefreshCounter();
        }
        return Task.CompletedTask;
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != base.Owner) return Task.CompletedTask;
        if (cardPlay.IsFirstInSeries)
        {
            CardsPlayedThisTurn++;
            RefreshCounter();
        }
        return Task.CompletedTask;
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (cardSource?.Owner != base.Owner) return 1m;
        if (!props.IsPoweredAttack()) return 1m;
        if (CardsPlayedThisTurn > 2) return 1m;

        Flash();
        return 2m;
    }

    private void RefreshCounter()
    {
        base.Status = CardsPlayedThisTurn <= 2 ? RelicStatus.Active : RelicStatus.Normal;
        InvokeDisplayAmountChanged();
    }

    public override Task AfterCombatEnd(CombatRoom _)
    {
        CardsPlayedThisTurn = 0;
        base.Status = RelicStatus.Normal;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }
}
