using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 天穹裙带：受到伤害时减少10点失去生命，并记录实际减少量。
/// 下回合结束时失去记录层数一半的生命并清空计数。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class FirmamentSash : TouhouAncientRelics
{
    public int MitigationTotal { get; set; }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Mitigation", 10)
    ];

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != base.Owner.Creature) return;
        if (result.UnblockedDamage <= 0) return;

        var reduction = (int)base.DynamicVars["Mitigation"].BaseValue;
        var actualMitigation = System.Math.Min(reduction, (int)result.UnblockedDamage);
        if (actualMitigation > 0)
        {
            // 治疗后追加减免的伤害
            await CreatureCmd.Heal(target, actualMitigation);
            MitigationTotal += actualMitigation;
        }
    }

    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side != base.Owner.Creature.Side) return;
        if (MitigationTotal <= 0) return;

        var damageToTake = MitigationTotal / 2;
        if (damageToTake > 0)
        {
            await CreatureCmd.Damage(
                new ThrowingPlayerChoiceContext(),
                base.Owner.Creature,
                damageToTake,
                ValueProp.Unpowered,
                base.Owner.Creature,
                null);
        }
        MitigationTotal = 0;
    }

    public override Task AfterCombatEnd(CombatRoom _)
    {
        MitigationTotal = 0;
        return Task.CompletedTask;
    }
}
