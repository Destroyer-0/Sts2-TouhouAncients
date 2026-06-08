using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 怨仇（对敌人施加的 Debuff）
/// - 敌人对来源（施加者）造成伤害降低 25%
/// - 来源打出攻击牌时，该敌人受到等同于怨仇层数的伤害，层数 -1
/// </summary>
public class YuanChouPower : TouhouAncientPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// 怨仇的来源（施加该 Debuff 的玩家生物）
    /// </summary>
    public Creature? Source { get; set; }

    /// <summary>
    /// 敌人对来源造成的伤害降低 25%
    /// </summary>
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // 只有敌人（owner）对来源（Source）造成伤害时才减伤
        if (dealer != base.Owner) return 1m;
        if (target != Source) return 1m;
        if (!props.IsPoweredAttack()) return 1m;
        return 0.75m;
    }
}
