using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using TouhouAncients.Scripts.monsters;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 烈毒护身：铃铃的防御能力（1 回合）。
/// 本回合所有对铃铃发起的攻击伤害（未格挡部分）都会转移到梅蒂欣·梅兰可莉身上。
/// 持续到梅蒂欣的下回合开始（下一个敌人回合的 AfterSideTurnStart 时移除）。
/// 实现参考原版 <c>DieForYouPower</c> 的 <see cref="ModifyUnblockedDamageTarget"/> 伤害重定向机制。
/// </summary>
public class GuardingPower : TouhouAncientPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override bool ShouldPlayVfx => false;

    /// <summary>
    /// 修改未格挡伤害的接收者：把对铃铃的攻击伤害转移给梅蒂欣·梅兰可莉。
    /// 仅当目标为本能力持有者（铃铃）、铃铃存活、梅蒂欣存活且伤害为攻击伤害（Powered Attack）时转移。
    /// </summary>
    public override Creature ModifyUnblockedDamageTarget(Creature target, decimal amount, ValueProp props, Creature? dealer)
    {
        if (target != base.Owner) return target;
        if (base.Owner.IsDead) return target;
        if (!props.IsPoweredAttack()) return target;

        Creature? medicine = FindMedicine();
        if (medicine == null || medicine.IsDead) return target;

        return medicine;
    }

    /// <summary>
    /// 梅蒂欣的下回合开始（敌人侧回合开始）时移除本能力。
    /// 施放发生在敌人回合中，当次敌人回合的 AfterSideTurnStart 早已触发，
    /// 因此这里移除的时机正好是"下一个敌人回合开始"，即效果持续一个完整回合。
    /// </summary>
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != CombatSide.Enemy) return;
        await PowerCmd.Remove(this);
    }

    /// <summary>
    /// 在同队中找到梅蒂欣·梅兰可莉（被转移目标）。
    /// </summary>
    private Creature? FindMedicine()
    {
        return base.Owner.CombatState?
            .GetTeammatesOf(base.Owner)
            .FirstOrDefault(c => c is { Monster: MedicineMelancholyMonster, IsDead: false });
    }
}
