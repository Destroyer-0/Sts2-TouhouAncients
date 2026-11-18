using System.Linq;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TouhouAncients.Scripts.cards;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 幸运的代价：挂在玩家身上的「幸运」戏法打出计数，图标上的数字即该玩家的张数。
/// 由 InabaTewiMonster 在玩家打出「幸运」戏法时递增（第一次打出才创建，故开场不存在）。
/// 取 PowerType.None：Debuff 会被人工制品挡下，Buff 会被「全人类的绯想天」这类加层效果放大，都会让计数失真。
/// </summary>
public class LuckyTrickCountPower : TouhouAncientPowerModel
{
    public const int CurseThreshold = 7;

    public override PowerType Type => PowerType.None;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (Amount < CurseThreshold || target != Owner || dealer?.IsEnemy != true || !props.IsPoweredAttack()) return 0m;
        return Owner.Player?.PlayerCombatState?.Hand.Cards.OfType<LuckyTrick>()
            .Sum(card => card.DynamicVars["ExtraDamage"].BaseValue) ?? 0m;
    }
}
