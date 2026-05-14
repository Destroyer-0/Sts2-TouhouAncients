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
/// 天穹裙带：受到来自敌人的伤害时减少10，并记录实际减少量。
/// 下回合结束时受到记录层数的伤害并清空计数。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class FirmamentSash : TouhouAncientRelics
{
    public int MitigationTotal { get; set; }

    public override bool ShowCounter => true;
    public override int DisplayAmount => MitigationTotal;
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Mitigation", 10)
    ];
    
    public override decimal ModifyHpLostAfterOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != base.Owner.Creature)
        {
            return amount;
        }
        
        if (!props.IsCardOrMonsterMove())
        {
            return amount;
        }

        if (dealer == Owner.Creature)
        {
            return amount;
        }

        var reduce = Math.Min(base.DynamicVars["Mitigation"].BaseValue, amount);
        MitigationTotal += (int)reduce;
        InvokeDisplayAmountChanged();
        return Math.Max(0m, amount - reduce);
    }

    public override Task AfterModifyingHpLostAfterOsty()
    {
        Flash();
        return Task.CompletedTask;
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != base.Owner.Creature.Side) return;
        if (MitigationTotal <= 0) return;
        if (MitigationTotal > 0)
        {
            Flash();
            await CreatureCmd.Damage(
                new ThrowingPlayerChoiceContext(),
                base.Owner.Creature,
                MitigationTotal,
                ValueProp.Unpowered,
                base.Owner.Creature,
                null);
            MitigationTotal = 0;
            InvokeDisplayAmountChanged();
        }
    }

    public override Task AfterCombatEnd(CombatRoom _)
    {
        MitigationTotal = 0;
        return Task.CompletedTask;
    }
}
