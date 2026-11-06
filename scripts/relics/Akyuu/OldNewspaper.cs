using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.relics.Akyuu;

/// <summary>
/// 神事旧闻：拾起时获得29点最大生命；此后每次受到未被格挡的伤害，失去3点最大生命，
/// 合计最多失去29点（最后一次只扣剩余的2点），扣完即耗尽。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class OldNewspaper : TouhouAncientRelics
{
    private const string GainKey = "MaxHpGain";
    private const string LossKey = "MaxHpLoss";
    private const string TotalKey = "MaxHpLossTotal";

    private int _maxHpLost;

    /// <summary>LoseMaxHp 溢出掉血会再触发本 Hook，靠此跳过那次自伤。</summary>
    private bool _losingMaxHp;

    public override bool IsUsedUp => MaxHpLost >= base.DynamicVars[TotalKey].IntValue;

    public override bool ShowCounter => !IsUsedUp;

    /// <summary>UI 显示剩余还能扣的最大生命。</summary>
    public override int DisplayAmount => Math.Max(0, base.DynamicVars[TotalKey].IntValue - MaxHpLost);

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(GainKey, 30m),
        new DynamicVar(LossKey, 2m),
        new DynamicVar(TotalKey, 30m),
    ];

    [SavedProperty]
    public int MaxHpLost
    {
        get => _maxHpLost;
        set
        {
            AssertMutable();
            _maxHpLost = value;
            InvokeDisplayAmountChanged();
            CheckIfUsedUp();
        }
    }

    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        var creature = base.Owner?.Creature;
        if (creature == null) return;

        await CreatureCmd.GainMaxHp(creature, base.DynamicVars[GainKey].BaseValue);
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != base.Owner?.Creature) return;
        if (result.UnblockedDamage <= 0) return;
        if (IsUsedUp || _losingMaxHp) return;

        var remaining = base.DynamicVars[TotalKey].IntValue - MaxHpLost;
        var amount = Math.Min(base.DynamicVars[LossKey].IntValue, remaining);

        Flash();
        _losingMaxHp = true;
        try
        {
            await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), target, amount, isFromCard: false);
            MaxHpLost += amount;
        }
        finally
        {
            _losingMaxHp = false;
        }
    }

    private void CheckIfUsedUp()
    {
        if (IsUsedUp) base.Status = RelicStatus.Disabled;
    }
}
