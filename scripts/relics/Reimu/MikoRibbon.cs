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
/// 巫女蝴蝶结：受到不高于 8 的未被格挡伤害时，将伤害降低至 1，
/// 并在本场战斗中减少 1 触发阈值，最低降低至 5。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class MikoRibbon : TouhouAncientRelics
{
    private const int _initialThreshold = 8;
    private const int _minThreshold = 5;

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
            return _currentThreshold;
        }
    }

    /// <summary>
    /// 当前触发阈值，每触发一次减少 1，最低降至 5。战斗开始与结束时重置。
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

    /// <summary>
    /// 受到的 HP 损失不高于当前阈值时，将伤害降低至 1，并减少 1 点触发阈值（最低 5）。
    /// </summary>
    public override decimal ModifyHpLostBeforeOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != base.Owner?.Creature) return amount;
        if (!CombatManager.Instance.IsInProgress) return amount;
        if (amount < 1) return amount;
        if (props.HasFlag(ValueProp.Unblockable)) return amount;
        if (amount > _currentThreshold) return amount;

        Flash();
        if (_currentThreshold > _minThreshold)
        {
            _currentThreshold--;
            InvokeDisplayAmountChanged();
        }
        return 1m;
    }

    public override Task AfterCombatEnd(CombatRoom _)
    {
        _currentThreshold = _initialThreshold;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }
}