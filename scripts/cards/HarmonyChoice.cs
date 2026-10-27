using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.cards;

/// <summary>演奏方式·协和。</summary>
[Pool(typeof(StatusCardPool))]
public sealed class HarmonyChoice : PerformanceChoiceCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("TurnAtmosphere", 5m),
        new DynamicVar("FullBlockAtmosphere", 9m),
    ];

    public override async Task OnChosen(PlayerChoiceContext choiceContext)
    {
        if (base.Owner?.Creature == null) return;
        await PowerCmd.Apply<HarmonyPower>(choiceContext, base.Owner.Creature, 1m, base.Owner.Creature, this);
    }
}
