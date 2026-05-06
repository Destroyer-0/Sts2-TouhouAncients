using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.relics;

[Pool(typeof(SharedRelicPool))]
public class FrogAmulet : TouhouAncientRelics
{
    private decimal _damageDealtThisTurn;

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult results, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (dealer != base.Owner.Creature) return;
        if (cardSource == null) return;

        _damageDealtThisTurn += results.TotalDamage;
    }

    public override async Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != base.Owner.Creature.Side) return;
        if (_damageDealtThisTurn <= 0) return;

        var power = (TheBombPower?)await PowerCmd.Apply<TheBombPower>(
            base.Owner.Creature, 2m, base.Owner.Creature, null);
        power?.SetDamage(_damageDealtThisTurn * 0.2m);

        _damageDealtThisTurn = 0;
    }
}
