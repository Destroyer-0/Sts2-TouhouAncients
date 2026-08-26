using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 博丽御札：受到不高于8的未被格挡伤害时，将伤害降低至1，
/// 并在本场战斗中减少1触发阈值，最低降低至5。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class HakureiAmulet : TouhouAncientRelics
{
    private const int _initialThreshold = 8;
    private const int _minThreshold = 5;

    public override bool ShowCounter => DisplayAmount >-1;

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
            return _currentThreshold;
        }
    }

    /// <summary>
    /// 当前触发阈值，每触发一次减少1，最低降至5。每场战斗重置。
    /// </summary>
    private int _currentThreshold = _initialThreshold;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Threshold", _initialThreshold),
        new DynamicVar("MinThreshold", _minThreshold)
    ];
    

    public override Task BeforeCombatStart()
    {
        _currentThreshold = _initialThreshold;
        InvokeDisplayAmountChanged();
        return base.BeforeCombatStart();
    }

    // public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    // {
    //     if (!participants.Contains(Owner.Creature) || side != base.Owner.Creature.Side)
    //     {
    //         return Task.CompletedTask;
    //     }
    //
    //     _currentThreshold = _initialThreshold;
    //     InvokeDisplayAmountChanged();
    //     return Task.CompletedTask;
    // }
    
    /// <summary>
    /// 受到的实际HP损失（破格挡后的伤害）不高于当前阈值时，将伤害降低至1，
    /// 并减少1点触发阈值（最低5）。
    /// </summary>
    public override decimal ModifyHpLostBeforeOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != base.Owner?.Creature) return amount;
        if (!CombatManager.Instance.IsInProgress) return amount;
        if (amount <= 0) return amount;
        //if (!props.IsCardOrMonsterMove()) return amount;
        if (props.HasFlag(ValueProp.Unblockable)) return amount;
        if (amount > _currentThreshold) return amount;

        Flash();
        if (_currentThreshold > _minThreshold)
        {
            _currentThreshold--;
            InvokeDisplayAmountChanged();
        }
        // // 将伤害降低至1，并减少触发阈值
        // _currentThreshold = Math.Max(_currentThreshold - 1, _minThreshold);
        return 1m;
    }

    /// <summary>
    /// 每场战斗结束后重置触发阈值。
    /// </summary>
    public override Task AfterCombatEnd(CombatRoom _)
    {
        _currentThreshold = _initialThreshold;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }
}
