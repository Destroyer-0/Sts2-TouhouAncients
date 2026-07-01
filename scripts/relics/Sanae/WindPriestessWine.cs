using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
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

    /// <summary>
    /// 追踪回合开始时获得的能量（最大能量）。
    /// </summary>
    public override async Task AfterEnergyReset(Player player)
    {
        if (player != base.Owner) return;
        if (player.PlayerCombatState == null) return;

        TouhouAncients_EnergyGainedCounter += player.PlayerCombatState.MaxEnergy;
        await TryDraw();
    }

    /// <summary>
    /// 追踪战斗中通过命令获得的能量（如肾上腺素、遗物等）。
    /// </summary>
    public override decimal ModifyEnergyGain(Player player, decimal amount)
    {
        if (player != base.Owner) return amount;
        TouhouAncients_EnergyGainedCounter += (int)amount;
        return amount;
    }

    /// <summary>
    /// ModifyEnergyGain 如果未修改获得的实际能量，此方法不会被调用。
    /// </summary>
    public override async Task AfterModifyingEnergyGain()
    {
        await TryDraw();
        await base.AfterModifyingEnergyGain();
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