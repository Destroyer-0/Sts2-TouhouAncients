using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 原初的神灵 — 在每场战斗开始时，获得1层无实体。
/// 在每回合开始时，获得等同于当前回合数的能量。
/// 第五回合开始时，获得孤注一掷。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class PrimalSpirit : TouhouAncientRelics
{
    private const string _triggerTurnKey = "TriggerTurn";

    /// <summary>
    /// 是否已到达触发回合，用于 DisplayAmount 显示
    /// </summary>
    private bool _hasTriggered;

    private bool HasTriggered
    {
        get => _hasTriggered;
        set
        {
            AssertMutable();
            _hasTriggered = value;
            InvokeDisplayAmountChanged();
        }
    }

    public override bool ShowCounter => DisplayAmount > -1;

    public override int DisplayAmount
    {
        get
        {
            if (!CombatManager.Instance.IsInProgress)
            {
                return -1;
            }
            if (base.IsCanonical)
            {
                return -1;
            }
            int triggerTurn = base.DynamicVars[_triggerTurnKey].IntValue;
            if (HasTriggered)
            {
                return triggerTurn;
            }
            int roundNumber = base.Owner.Creature.CombatState.RoundNumber;
            if (roundNumber >= triggerTurn)
            {
                return -1;
            }
            return roundNumber;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(_triggerTurnKey, 5m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<TheGambitPower>()
    ];

    public override async Task BeforeCombatStart()
    {
        HasTriggered = false;
    }

    /// <summary>
    /// 每回合开始时获得等同于当前回合数的能量
    /// </summary>
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != base.Owner.Creature.Side) return;
        
        if (!participants.Contains(Owner.Creature))
        {
            return;
        }
        var round = combatState.RoundNumber;

        // 获得等于回合数的能量
        if (round > 0)
        {
            Flash();
            await PlayerCmd.GainEnergy(round, base.Owner);
        }

        // 到达触发回合时标记、刷新 UI
        int triggerTurn = base.DynamicVars[_triggerTurnKey].IntValue;
        if (round == triggerTurn)
        {
            base.Status = RelicStatus.Active;
        }
        InvokeDisplayAmountChanged();

        // 第五回合开始时获得孤注一掷
        if (round == triggerTurn)
        {
            await PowerCmd.Apply<TheGambitPower>(new ThrowingPlayerChoiceContext(), base.Owner.Creature, 1m, base.Owner.Creature, null);
        }
    }

    public override Task AfterCombatEnd(MegaCrit.Sts2.Core.Rooms.CombatRoom _)
    {
        HasTriggered = false;
        base.Status = RelicStatus.Normal;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }
}
