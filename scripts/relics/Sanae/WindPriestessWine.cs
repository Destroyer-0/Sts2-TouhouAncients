using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using TouhouAncients.Scripts.Hooks;

namespace TouhouAncients.Scripts.relics;

[Pool(typeof(EventRelicPool))]
public class WindPriestessWine : TouhouAncientRelics, IPlayerEnergyGainedListener
{
    public override bool IsAllowed(IRunState runState) => runState.Players.Count == 1;


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

    public override async Task AfterEnergyReset(Player player)
    {
        if (player != base.Owner) return;
        if (player.PlayerCombatState == null) return;

        AddEnergyGain(player.PlayerCombatState.MaxEnergy);

        await TryDraw(player);
    }

    public async Task AfterPlayerEnergyGained(PlayerEnergyGainContext context)
    {
        if (context.Player != base.Owner) return;

        AddEnergyGain(context.Amount);
        await TryDraw(context.Player);
    }

    private async Task TryDraw(Player player)
    {
        while (TouhouAncients_EnergyGainedCounter >= DynamicVars.Energy.IntValue)
        {
            TouhouAncients_EnergyGainedCounter -= DynamicVars.Energy.IntValue;
            Flash();
            await CardPileCmd.Draw(
                new ThrowingPlayerChoiceContext(),
                DynamicVars.Cards.IntValue,
                player,
                fromHandDraw: false);
        }
    }

    public void AddEnergyGain(int amount)
    {
        TouhouAncients_EnergyGainedCounter += amount;
    }
}
