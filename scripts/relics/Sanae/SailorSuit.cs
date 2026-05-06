using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

[Pool(typeof(SharedRelicPool))]
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
        await PowerCmd.Apply<ArtifactPower>(base.Owner.Creature, 1m, base.Owner.Creature, null);
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
            await PowerCmd.Apply<T>(enemy, amount, base.Owner.Creature, null);
        }
    }

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
            modifiedAmount = amount;
            return false;
        }
        if (Owner.Creature.CombatState == null)
        {
            return false;
        }
        if (canonicalPower is FrailPower or VulnerablePower or WeakPower)
        {
            modifiedAmount = 0;
            return true;
        }
        return false;
    }
}
