using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using TouhouAncients.Scripts.monsters;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 双生 — 紫苑的被动能力。
/// 效果1：每回合为女苑提供 8 点格挡（在玩家侧回合开始时触发，参考 RampartPower）。
/// 效果2：紫苑死亡时，在 2 回合后以 50 HP 复活（参考 ReattachPower）。
/// 无限复活次数。
/// 女苑被击倒时，此能力将被移除。
/// </summary>
public class TwinSoulPower : TouhouAncientPowerModel
{
    private class Data
    {
        public bool isReviving;
        public int reviveCountdown;
    }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private bool IsReviving
    {
        get => GetInternalData<Data>().isReviving;
        set => GetInternalData<Data>().isReviving = value;
    }

    private int ReviveCountdown
    {
        get => GetInternalData<Data>().reviveCountdown;
        set => GetInternalData<Data>().reviveCountdown = value;
    }

    protected override object InitInternalData()
    {
        return new Data { reviveCountdown = 0 };
    }

    /// <summary>
    /// 效果1：每回合为女苑提供格挡（在玩家侧回合开始时）。
    /// 参考 RampartPower。
    /// </summary>
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        
        if (side != CombatSide.Player || CombatManager.Instance.PlayersTakingExtraTurn.Count > 0)
        {
            return;
        }
        IEnumerable<Creature> enumerable = base.CombatState.Enemies.Where((Creature c) => c.Monster is YorigamiJoon);
        foreach (Creature item in enumerable)
        {
            await CreatureCmd.GainBlock(item, base.Amount, ValueProp.Unpowered, null);
        }
    }

    /// <summary>
    /// 效果2：倒计时与复活。
    /// 在敌人侧回合结束时推进倒计时。
    /// </summary>
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner))
        {
            return;
        }
        if (side != base.Owner.Side) return;
        if (!IsReviving) return;

        ReviveCountdown--;
        if (ReviveCountdown <= 0)
        {
            await DoReattach();
        }
    }

    /// <summary>
    /// 死亡时：标记为复活中状态（不真正死亡），切换到自修复状态显示治疗意图。
    /// </summary>
    public override Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature,
        bool wasRemovalPrevented, float deathAnimLength)
    {
        if (wasRemovalPrevented || base.Owner != creature) return Task.CompletedTask;

        IsReviving = true;
        ReviveCountdown = 2;

        if (base.Owner.Monster is YorigamiShion shion)
        {
            shion.SetMoveImmediate(shion.SelfRepairState);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 执行复活：治疗 50 HP，清除复活标记，切回正常状态机。
    /// </summary>
    private async Task DoReattach()
    {
        IsReviving = false;
        await CreatureCmd.Heal(base.Owner, 50m);

        if (base.Owner.Monster is YorigamiShion shion)
        {
            shion.ExitSelfRepairState();
        }
    }

    /// <summary>
    /// 死亡后不将生物从战斗中移除（复活需要保留）。
    /// </summary>
    public override bool ShouldCreatureBeRemovedFromCombatAfterDeath(Creature creature)
    {
        return false;
    }

    /// <summary>
    /// 死亡后不移除此 Power（复活需要保留）。
    /// </summary>
    public override bool ShouldPowerBeRemovedAfterOwnerDeath()
    {
        return false;
    }

    /// <summary>
    /// 只有不在复活倒计时中时才视为致命死亡。
    /// </summary>
    public override bool ShouldOwnerDeathTriggerFatal()
    {
        return !IsReviving;
    }
}
