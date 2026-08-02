using System.Collections.Generic;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 龙脉之皿：标记1条路线。进入路线上的节点获得{Gold}金币，
/// 如果是战斗则战斗开始时获得{Strength}力量{Strength}敏捷，且额外掉落{CardRewards}组卡牌奖励。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class DragonVeinVessel : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Gold", 33m),
        new DynamicVar("Strength", 1m),
        new DynamicVar("Dexterity", 1m),
        new DynamicVar("CardRewards", 1m),
    ];
}
