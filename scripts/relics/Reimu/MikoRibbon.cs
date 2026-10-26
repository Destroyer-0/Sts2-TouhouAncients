using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 巫女蝴蝶结：受到来自敌人的伤害减少 2。
/// 每场战斗中，阻止每个敌人第一次尝试获得的永久/临时力量。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class MikoRibbon : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Reduction", 2m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<StrengthPower>()];

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer,
        CardModel? cardSource, CardPlay? cardPlay)
    {
        if (base.Owner?.Creature == null) return 0m;
        if (target != base.Owner.Creature) return 0m;
        if (dealer == null || !dealer.IsEnemy) return 0m;

        return -base.DynamicVars["Reduction"].BaseValue;
    }

    /// <summary>
    /// 第一次力量尝试前挂上标记 Power；之后由 TryModifyPowerAmountReceived 消耗它完成阻止。
    /// </summary>
    public override async Task BeforePowerAmountChanged(PowerModel power, decimal amount, Creature target,
        Creature? applier, CardModel? cardSource)
    {
        if (amount <= 0m) return;
        if (!IsStrengthGain(power)) return;
        if (!target.IsEnemy || !target.IsAlive) return;
        if (base.Owner?.Creature == null) return;
        if (target.GetPower<MikoRibbonMarkPower>() != null) return;

        await PowerCmd.Apply<MikoRibbonMarkPower>(
            new ThrowingPlayerChoiceContext(), target, 1m, base.Owner.Creature, null, silent: true);
    }

    public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount,
        Creature? applier, out decimal modifiedAmount)
    {
        modifiedAmount = amount;
        if (amount <= 0m) return false;
        if (!IsStrengthGain(canonicalPower)) return false;
        if (!target.IsEnemy) return false;

        MikoRibbonMarkPower? mark = target.GetPower<MikoRibbonMarkPower>();
        if (mark == null || mark.Consumed) return false;

        // 只消耗一次：多个持有者的后续 listener 看到 Consumed 后放行
        mark.Consume();
        Flash();
        modifiedAmount = 0m;
        return true;
    }

    // 临时力量整条阻止（含回合结束的扣除），所以连临时力量 Power 一起拦；
    // mod 的临时力量继承 ITemporaryPower，不一定是原版 TemporaryStrengthPower
    private static bool IsStrengthGain(PowerModel power) =>
        power is StrengthPower ||
        (power is ITemporaryPower temporary && power.Type == PowerType.Buff &&
         temporary.InternallyAppliedPower is StrengthPower);
}
