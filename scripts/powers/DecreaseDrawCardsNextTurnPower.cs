using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TouhouAncients.Scripts.powers;

public class DecreaseDrawCardsNextTurnPower : TouhouAncientPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override Decimal ModifyHandDraw(Player player, Decimal count)
    {
        return player != this.Owner.Player || this.AmountOnTurnStart == 0 ? count : count - (Decimal) this.Amount;
    }
    public override async Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        DecreaseDrawCardsNextTurnPower power = this;
        if (!participants.Contains<Creature>(power.Owner) || power.AmountOnTurnStart == 0)
            return;
        await PowerCmd.Remove((PowerModel) power);
    }
}