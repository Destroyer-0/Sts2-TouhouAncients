using System.Threading.Tasks;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.relics;

[Pool(typeof(EventRelicPool))]
public class WindPriestessWine : TouhouAncientRelics
{
    [SavedProperty]
    private int TouhouAncients_EnergyGainedCounter
    {
        get => _pendingGain;
        set
        {
            AssertMutable();
            _pendingGain = value;
            InvokeDisplayAmountChanged();
        }
    }

    public override bool ShowCounter => true;
    public override int DisplayAmount => TouhouAncients_EnergyGainedCounter;

    private int _pendingGain;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(9),
        new CardsVar(2)
    ];

    public override Task BeforeCombatStart()
    {
        var combatState = base.Owner.PlayerCombatState;
        if (combatState == null) return Task.CompletedTask;
        combatState.EnergyChanged += OnEnergyChanged;
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom _)
    {
        if (base.Owner.PlayerCombatState != null)
        {
            base.Owner.PlayerCombatState.EnergyChanged -= OnEnergyChanged;
        }
        return Task.CompletedTask;
    }

    private void OnEnergyChanged(int oldEnergy, int newEnergy)
    {
        if (newEnergy <= oldEnergy) return;
        TouhouAncients_EnergyGainedCounter += newEnergy - oldEnergy;
        _ = TryDraw();
    }

    private async Task TryDraw()
    {
        while (TouhouAncients_EnergyGainedCounter >= DynamicVars.Energy.IntValue)
        {
            TouhouAncients_EnergyGainedCounter -= DynamicVars.Energy.IntValue;
            Flash();
            await CardPileCmd.Draw(new ThrowingPlayerChoiceContext(), DynamicVars.Cards.IntValue, base.Owner, fromHandDraw: false);
        }
    }
}