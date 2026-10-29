using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.cards;

/// <summary>演奏方式·断奏。</summary>
[Pool(typeof(StatusCardPool))]
public sealed class StaccatoChoice : PerformanceChoiceCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("DamagePerAtmosphere", 3m),
        new DynamicVar("AtmosphereGain", 1m),
        new DynamicVar("DeathAtmosphere", 14m),
    ];

    public override async Task OnChosen(PlayerChoiceContext choiceContext)
    {
        if (base.Owner?.Creature == null) return;
        await PowerCmd.Apply<StaccatoPower>(choiceContext, base.Owner.Creature, 1m, base.Owner.Creature, this);
    }
}
