using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TouhouAncients.Scripts.powers;

public abstract class TouhouAncientTemporaryStrengthPower : TouhouAncientTemporaryPower
{
    public override LocString Description
    {
        get
        {
            return new LocString("powers",
                this.IsPositive ? "TEMPORARY_STRENGTH_POWER.description" : "TEMPORARY_STRENGTH_DOWN.description");
        }
    }

    protected override string SmartDescriptionLocKey
    {
        get
        {
            return !this.IsPositive
                ? "TEMPORARY_STRENGTH_DOWN.smartDescription"
                : "TEMPORARY_STRENGTH_POWER.smartDescription";
        }
    }
    


    
    
    public override async Task BeforeApplied(
        Creature target,
        Decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (ShouldIgnoreNextInstance)
        {
            ShouldIgnoreNextInstance = false;
        }
        else
        {
            StrengthPower? strengthPower =
                await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), target, (Decimal)this.Sign * amount, applier, cardSource, true);
        }
    }

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        Decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        var temporaryStrengthPower = this;
        if (amount == (Decimal)temporaryStrengthPower.Amount || power != temporaryStrengthPower)
            return;
        if (temporaryStrengthPower.ShouldIgnoreNextInstance)
        {
            temporaryStrengthPower.ShouldIgnoreNextInstance = false;
        }
        else
        {
            StrengthPower? strengthPower = await PowerCmd.Apply<StrengthPower>(choiceContext, temporaryStrengthPower.Owner,
                (Decimal)temporaryStrengthPower.Sign * amount, applier, cardSource, true);
        }
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        var power = this;
        if (side != power.Owner.Side)
            return;
        power.Flash();
        await PowerCmd.Remove((PowerModel)power);
        StrengthPower strengthPower = await PowerCmd.Apply<StrengthPower>(choiceContext, power.Owner,
            (Decimal)(-power.Sign * power.Amount), power.Owner, (CardModel)null);
    }
}