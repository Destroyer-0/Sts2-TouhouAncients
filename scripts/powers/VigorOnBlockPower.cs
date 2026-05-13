using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 罡气：在本回合将格挡掉的攻击伤害转化为等量活力。
/// 多次获得时转化倍率叠加，回合数始终为1。
/// </summary>
public class VigorOnBlockPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != base.Owner) return;
        if (result.BlockedDamage <= 0) return;
        if (!props.IsPoweredAttack()) return;
        if (dealer == null) return;

        // 将格挡掉的伤害 × 层数 转化为活力
        var vigorAmount = (int)(result.BlockedDamage * base.Amount / 100);
        if (vigorAmount > 0)
        {
            await PowerCmd.Apply<VigorPower>(base.Owner, vigorAmount, base.Owner, null);
        }
    }

    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side == base.Owner.Side)
        {
            await PowerCmd.Remove(this);
        }
    }
}
