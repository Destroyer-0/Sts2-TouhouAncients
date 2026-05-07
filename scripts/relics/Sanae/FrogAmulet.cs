using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.relics;

[Pool(typeof(SharedRelicPool))]
public class FrogAmulet : TouhouAncientRelics
{
    private const decimal extraDamagePercent = 0.8m;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("ExtraDamage", extraDamagePercent)];
   
    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult results, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (dealer != base.Owner.Creature) return;
        if (cardSource == null) return;

        var damageDealtThisTurn = Mathf.FloorToInt(results.TotalDamage*(1-(float)extraDamagePercent));
        if (damageDealtThisTurn <= 0) return;
        var power = (TheBombPower?)await PowerCmd.Apply<TheBombPower>(
            base.Owner.Creature, 1m, base.Owner.Creature, null);
        power?.SetDamage(damageDealtThisTurn);
    }
}
