using System.Collections.Generic;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 邪仙发簪：每场战斗你首次死亡时，恢复{Percent}%生命值，之后的回合由霍青娥接管。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class WickedHermitHairpin : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Percent", 50m),
    ];
}
