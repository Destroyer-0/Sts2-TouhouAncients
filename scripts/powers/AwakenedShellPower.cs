using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TouhouAncients.Scripts.powers;

/// <summary>「觉醒之贝?」：贝子自己的回合结束时回复生命。</summary>
public sealed class AwakenedShellPower : TouhouAncientPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (base.Owner.IsDead || !participants.Contains(base.Owner)) return;

        Flash();
        await CreatureCmd.Heal(base.Owner, base.Amount);
    }
}
