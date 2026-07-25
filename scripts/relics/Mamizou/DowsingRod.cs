using System.Collections.Generic;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 寻龙尺：你没有选择的卡牌奖励将会被记录。你可以在休息处雇佣纳兹琳寻宝。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class DowsingRod : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [];
}
