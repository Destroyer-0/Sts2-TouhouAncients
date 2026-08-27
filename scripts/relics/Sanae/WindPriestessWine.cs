using System.Threading.Tasks;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.relics;

[Pool(typeof(EventRelicPool))]
public class WindPriestessWine : TouhouAncientRelics
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

        await TryDraw();
    }

    public override decimal ModifyEnergyGain(Player player, decimal amount)
    {
        if (player == base.Owner) TouhouAncients_EnergyGainedCounter += (int)amount;
        return amount;
    }
    
    public override async Task AfterModifyingEnergyGain()
    {
        await TryDraw();
    }

    private async Task TryDraw()
    {
        if (!LocalContext.NetId.HasValue) return;

        while (TouhouAncients_EnergyGainedCounter >= DynamicVars.Energy.IntValue)
        {
            TouhouAncients_EnergyGainedCounter -= DynamicVars.Energy.IntValue;
            Flash();
            var ctx = new HookPlayerChoiceContext(
                base.Owner, LocalContext.NetId.Value, GameActionType.CombatPlayPhaseOnly);

            await CardPileCmd.Draw(ctx, DynamicVars.Cards.IntValue, base.Owner, fromHandDraw: false);
        }
    }

    public void AddEnergyGain(int amount)
    {
        TouhouAncients_EnergyGainedCounter += amount;
    }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyEnergyGain))]
public static class WindPriestessWine_ModifyEnergyGain_Patch
{
    [HarmonyPostfix]
    private static void Postfix(Player player, ref IEnumerable<AbstractModel> modifiers)
    {
        var relic = player?.GetRelic<WindPriestessWine>();
        if (relic != null && !modifiers.Contains(relic))
            modifiers = modifiers.Append(relic).ToList();
    }
}