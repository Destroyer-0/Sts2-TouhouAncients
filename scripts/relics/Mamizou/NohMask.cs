using System.Collections.Generic;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 能乐假面：每回合开始时随机将{EmotionCards}张手牌在本回合添加喜、怒、哀、乐情绪。
/// 当你打出喜、怒、哀、乐情绪各{EmotionThreshold}张后，恢复能量并抽{Cards}张牌。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class NohMask : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("EmotionThreshold", 1m),
        new DynamicVar("Cards", 1m),
        new EnergyVar(1),
    ];
}
