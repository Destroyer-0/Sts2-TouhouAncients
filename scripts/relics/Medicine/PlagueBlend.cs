using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 荒疫特调：在每个回合开始时获得1能量。第3回合开始时，给予自身4层中毒。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class PlagueBlend : TouhouAncientRelics
{
    private const int TriggerTurn = 3;
    private const decimal PoisonAmount = 4m;

    public override bool HasUponPickupEffect => false;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(1),
        new DynamicVar("TriggerTurn",3m),
        new DynamicVar("PoisonAmount", PoisonAmount),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.ForEnergy(this),
        HoverTipFactory.FromPower<PoisonPower>(),
    ];


    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        if (player != base.Owner)
            return amount;
        return amount + base.DynamicVars.Energy.IntValue;
    }
    // public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    // {
    //     if (player != base.Owner) return;
    //     if (player.Creature.CombatState == null) return;
    //
    //     _turnsPassed++;
    //     // 第3回合开始时，给予自身4层中毒
    //     if (_turnsPassed == TriggerTurn)
    //     {
    //         Flash();
    //         await PowerCmd.Apply<PoisonPower>(player.Creature, PoisonAmount, player.Creature, null);
    //     }
    // }
    
    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side == base.Owner.Creature.Side && combatState.RoundNumber == TriggerTurn)
        {
            Flash();
            await PowerCmd.Apply<PoisonPower>(Owner.Creature, PoisonAmount, Owner.Creature, null);
        }
    }
}
