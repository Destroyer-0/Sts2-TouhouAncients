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
/// 效果2：紫苑死亡时，如果女苑还存活，在 2 回合后以 50 HP 复活（参考 ReattachPower）。
/// 无限复活次数。
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
    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side != CombatSide.Player) return;

        IEnumerable<Creature> joonCreatures = base.CombatState.Enemies
            .Where(c => c.Monster is YorigamiJoon && !c.IsDead);
        foreach (Creature joon in joonCreatures)
        {
            await CreatureCmd.GainBlock(joon, 8m, ValueProp.Unpowered, null);
        }
    }

    /// <summary>
    /// 效果2：倒计时与复活。
    /// 在敌人侧回合结束时推进倒计时。
    /// </summary>
    public override async Task AfterSideTurnEnd(CombatSide side, CombatState combatState)
    {
        if (side != base.Owner.Side) return;
        if (!IsReviving) return;

        ReviveCountdown--;
        if (ReviveCountdown <= 0)
        {
            await DoReattach();
        }
    }

    /// <summary>
    /// 死亡时：如果女苑还存活，则标记为复活中状态（不真正死亡）。
    /// </summary>
    public override Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature,
        bool wasRemovalPrevented, float deathAnimLength)
    {
        if (wasRemovalPrevented || base.Owner != creature) return Task.CompletedTask;

        // 检查女苑是否存活
        bool joonAlive = base.CombatState.Enemies
            .Any(c => c.Monster is YorigamiJoon && !c.IsDead);

        if (joonAlive)
        {
            IsReviving = true;
            ReviveCountdown = 2;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 执行复活：治疗 50 HP 并将复活标记清除。
    /// </summary>
    private async Task DoReattach()
    {
        // 再次检查女苑是否还存活
        bool joonAlive = base.CombatState.Enemies
            .Any(c => c.Monster is YorigamiJoon && !c.IsDead);

        if (!joonAlive) return;

        IsReviving = false;
        await CreatureCmd.Heal(base.Owner, 50m);
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
