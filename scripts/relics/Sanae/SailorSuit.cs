using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

[Pool(typeof(EventRelicPool))]
public class SailorSuit : TouhouAncientRelics
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<ArtifactPower>(),
        HoverTipFactory.FromPower<FrailPower>(),
        HoverTipFactory.FromPower<WeakPower>(),
        HoverTipFactory.FromPower<VulnerablePower>(),
    ];
    public override async Task BeforeCombatStart()
    {
        Flash();
        await PowerCmd.Apply<ArtifactPower>(new ThrowingPlayerChoiceContext(),base.Owner.Creature, 1m, base.Owner.Creature, null);
    }

    public override async Task BeforePowerAmountChanged(PowerModel power, decimal amount, Creature target, Creature? applier, CardModel? cardSource)
    {
        if (target != base.Owner.Creature) return;
        if (Owner.Creature.CombatState == null) return;
        if (amount <= 0) return;

        if (power is FrailPower)
        {
            await ApplyReflected<FrailPower>(amount);
        }
        else if (power is VulnerablePower)
        {
            await ApplyReflected<VulnerablePower>(amount);
        }
        else if (power is WeakPower)
        {
            await ApplyReflected<WeakPower>(amount);
        }
    }

    private async Task ApplyReflected<T>(decimal amount) where T : PowerModel
    {
        if (Owner.Creature.CombatState == null) return;
        
        var enemies = base.Owner.Creature.CombatState
            .GetOpponentsOf(base.Owner.Creature)
            .Where(c => c.IsAlive);

        foreach (var enemy in enemies)
        {
            await PowerCmd.Apply<T>(new ThrowingPlayerChoiceContext(), enemy, amount, base.Owner.Creature, null);
        }
    }

    private bool _preventArtifactDecrement;

    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        Decimal amount,
        Creature? _,
        out Decimal modifiedAmount)
    {
        modifiedAmount = amount;

        if (target != Owner.Creature)
        {
            return false;
        }
        if (Owner.Creature.CombatState == null)
        {
            return false;
        }

        // When ArtifactPower tries to decrement itself after blocking a debuff we handle,
        // prevent the decrement to save Artifact stacks.
        if (canonicalPower is ArtifactPower && amount < 0 && _preventArtifactDecrement)
        {
            modifiedAmount = 0;
            _preventArtifactDecrement = false;
            return true;
        }

        // Nullify Frail/Vulnerable/Weak on self, and set flag to protect ArtifactPower
        if (canonicalPower is FrailPower or VulnerablePower or WeakPower)
        {
            modifiedAmount = 0;
            _preventArtifactDecrement = true;
            return true;
        }

        return false;
    }

    public override async Task AfterModifyingPowerAmountReceived(PowerModel power)
    {
        // Safety: clear flag after the debuff processing cycle ends,
        // in case ArtifactPower decrement was somehow skipped.
        if (power is FrailPower or VulnerablePower or WeakPower)
        {
            _preventArtifactDecrement = false;
        }
    }
}
