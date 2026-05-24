using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
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
    private const decimal extraDamagePercent = 0.75m;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("ExtraDamage", extraDamagePercent)];

    private TheBombPower? _bombPower;
    private int damageDealtThisTurn = 0;

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer,
        DamageResult results, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (dealer != base.Owner.Creature) return;
        if (cardSource == null) return;

        damageDealtThisTurn += Mathf.FloorToInt(results.TotalDamage * (1 - (float)extraDamagePercent));
        if (damageDealtThisTurn <= 0) return;
        if (_bombPower == null)
        {
            _bombPower = (TheBombPower?)await PowerCmd.Apply<TheBombPower>(
                base.Owner.Creature, 2m, base.Owner.Creature, null);
        }
        _bombPower?.SetDamage(damageDealtThisTurn);
    }

    public override Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner) return Task.CompletedTask;
        _bombPower = null;
        damageDealtThisTurn = 0;
        return Task.CompletedTask;
    }
}