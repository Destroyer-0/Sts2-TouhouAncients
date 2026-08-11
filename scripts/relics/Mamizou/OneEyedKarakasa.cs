using System.Collections.Generic;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 独眼唐伞：每当你斩杀一名敌人时，获得{Charges}计数，如果此时为战斗的第一回合则改为{FirstTurnCharges}。
/// 你可以在休息处花费{UpgradeCost}计数额外升级{UpgradeCount}张牌。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class OneEyedKarakasa : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Charges", 1m),
        new DynamicVar("FirstTurnCharges", 3m),
        new DynamicVar("UpgradeCost", 4m),
        new DynamicVar("UpgradeCount", 1m),
    ];
}
