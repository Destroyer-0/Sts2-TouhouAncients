using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.relics.Akyuu;

/// <summary>
/// 神事旧闻：拾起时获得30点最大生命；此后每次受到未被格挡的伤害，失去2点最大生命，
/// 累计失去30点最大生命后失效——任何来源的上限扣减都计入，等于替玩家抵消一部分。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class OldNewspaper : TouhouAncientRelics
{
    private const string GainKey = "MaxHpGain";
    private const string LossKey = "MaxHpLoss";

    private int _maxHpLost;

    public override bool IsUsedUp => MaxHpLost >= base.DynamicVars[GainKey].IntValue;

    public override bool ShowCounter => !IsUsedUp;

    /// <summary>UI 显示剩余还能扣的最大生命。</summary>
    public override int DisplayAmount => Math.Max(0, base.DynamicVars[GainKey].IntValue - MaxHpLost);

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(GainKey, 30m),
        new DynamicVar(LossKey, 1m),
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

    /// <summary>由 OldNewspaperMaxHpPatch 推送；任何来源的上限降低都计入阈值（含本遗物自身扣减）。</summary>
    internal void NotifyMaxHpLost(int delta)
    {
        if (delta <= 0 || IsUsedUp) return;
        MaxHpLost = Math.Clamp(MaxHpLost + delta, 0, base.DynamicVars[GainKey].IntValue);
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != base.Owner?.Creature) return;
        if (!props.IsPoweredAttack()) return;
        if (result.UnblockedDamage <= 0) return;
        if (IsUsedUp) return;

        var remaining = base.DynamicVars[GainKey].IntValue - MaxHpLost;
        if (remaining <= 0) return;
        var amount = Math.Min(base.DynamicVars[LossKey].IntValue, remaining);
        await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), target, amount, cardSource: null);
    }

    private void CheckIfUsedUp()
    {
        if (IsUsedUp) base.Status = RelicStatus.Disabled;
    }
}

/// <summary>
/// 神事旧闻的阈值计数来源。
/// 本体没有 max HP 变化 Hook；`Creature.MaxHpChanged` 订阅又不随存档恢复（读档后是新遗物实例）。
/// 挂在 SetMaxHp 而非 SetMaxHpInternal：联机同步走 SyncWithSerializedPlayer→SetMaxHpInternal，
/// 那是「恢复存档上限」不是掉上限，绕开它就无需额外屏蔽开关。
/// </summary>
[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.SetMaxHp))]
public static class OldNewspaperMaxHpPatch
{
    private static void Prefix(Creature creature, out int __state)
    {
        __state = creature.MaxHp;
    }

    // SetMaxHp 唯一 await 在写入之后，Postfix 时 MaxHp 已是新值
    private static void Postfix(Creature creature, int __state)
    {
        var player = creature.Player;
        if (player == null) return;

        var delta = __state - creature.MaxHp;
        if (delta <= 0) return;

        foreach (var relic in player.Relics)
        {
            if (relic is OldNewspaper newspaper) newspaper.NotifyMaxHpLost(delta);
        }
    }
}