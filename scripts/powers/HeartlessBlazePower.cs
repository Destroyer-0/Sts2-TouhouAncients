using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using TouhouAncients.Scripts.monsters;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 无心璨炎：纯狐的破防标记。
/// 生命值被压到阈值以下时移除自身；只要被移除（含其它手段），
/// 清除全部力量并进入二阶段——由 <see cref="AfterRemoved"/> 统一处理。
/// 纯狐造成未被格挡的伤害会抬高阈值，一次攻击只算一次。
/// Amount 为生命值触发阈值，与怪物生命同样按玩家数与幕系数放大（参考原版犁地能力）。
/// </summary>
public sealed class HeartlessBlazePower : TouhouAncientPowerModel
{
    private const string _increaseKey = "Increase";

    private const decimal _baseIncrease = 50m;

    /// <summary>阈值被提高过的次数，破防台词据此选择。</summary>
    private int _raiseCount;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldScaleInMultiplayer => true;

    /// <summary>图标暂用怨仇的图，后续替换。</summary>
    public override string? CustomPackedIconPath => TouhouAncientCmd.CheckPathExists("res://images/icon/power/YuanChouPower.png");
    public override string? CustomBigIconPath => TouhouAncientCmd.CheckPathExistsWithFallback2("res://images/icon/power/BigIcon/YuanChouPower.png", CustomPackedIconPath);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<StrengthPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar(_increaseKey, _baseIncrease)];

    /// <summary>增量与阈值一样按人数与幕系数放大，显示值也随之放大（参考 MagicianPower 的 Heal）。</summary>
    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        ICombatState? combatState = base.Owner?.CombatState;
        DynamicVar increase = base.DynamicVars[_increaseKey];
        if (combatState == null || combatState.Players.Count <= 1) return Task.CompletedTask;
        if (!base.Owner!.IsPrimaryEnemy && !base.Owner.IsSecondaryEnemy) return Task.CompletedTask;

        increase.BaseValue = GetScaledAmountForMultiplayer(combatState, applier, increase.BaseValue, base.Owner, cardSource);
        return Task.CompletedTask;
    }

    /// <summary>照原版吮吸（SuckPower）按段判定：一次攻击命中多个玩家/多段也只涨一次。</summary>
    public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
    {
        if (command.Attacker != base.Owner || command.TargetSide == base.Owner.Side) return;
        if (!command.Results.Any(hit => hit.Any(r => r.UnblockedDamage > 0))) return;

        _raiseCount++;
        await PowerCmd.ModifyAmount(choiceContext, this, base.DynamicVars[_increaseKey].BaseValue, base.Owner, null);

        // 阈值涨过当前生命值时立即破防
        if (base.Owner.CurrentHp <= base.Amount) await Trigger();
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != base.Owner || result.UnblockedDamage <= 0) return;
        if (target.CurrentHp > base.Amount) return;
        await Trigger();
    }

    /// <summary>只移除自身，二阶段统一由 AfterRemoved 处理。</summary>
    private async Task Trigger()
    {
        Flash();
        await PowerCmd.Remove(this);
    }

    /// <summary>死亡/清场走的是跳过 AfterRemoved 的路径，所以这里只在真正被移除时进二阶段。</summary>
    public override async Task AfterRemoved(Creature oldOwner)
    {
        if (oldOwner.IsDead || oldOwner.Monster is not JunkoMonster junko) return;
        await PowerCmd.Remove<StrengthPower>(oldOwner);
        junko.EnterPhaseTwo(_raiseCount > 1);
    }
}
