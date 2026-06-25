using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 提神咖啡：每回合首次能量为0时，获得1能量。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class BitterCoffee : TouhouAncientRelics
{
    private bool _triggeredThisTurn;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.ForEnergy(this)];

    public override Task BeforeCombatStart()
    {
        var combatState = base.Owner.PlayerCombatState;
        if (combatState == null) return Task.CompletedTask;
        combatState.EnergyChanged += CheckRemainingEnergy;
        return Task.CompletedTask;
    }

    private void CheckRemainingEnergy(int oldEnergy, int newEnergy)
    {
        if (_triggeredThisTurn) return;
        if (oldEnergy <= 0) return;
        if (newEnergy != 0) return;

        _triggeredThisTurn = true;
        Flash();
        _ = PlayerCmd.GainEnergy(base.DynamicVars.Energy.BaseValue, base.Owner);
    }

    public override Task AfterEnergyReset(Player player)
    {
        if (player != base.Owner) return Task.CompletedTask;
        _triggeredThisTurn = false;
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom _)
    {
        _triggeredThisTurn = false;

        if (base.Owner.PlayerCombatState != null)
        {
            base.Owner.PlayerCombatState.EnergyChanged -= CheckRemainingEnergy;
        }

        return Task.CompletedTask;
    }
}