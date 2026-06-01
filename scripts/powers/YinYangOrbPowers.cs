using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 阴阳玉·阴：打出技能牌时获得的临时力量。
/// </summary>
public class YinYangOrbStrengthPower : TouhouAncientTemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Relic<YinYangOrb>();
}

/// <summary>
/// 阴阳玉·阳：打出攻击牌时获得的临时敏捷。
/// </summary>
public class YinYangOrbDexterityPower : TouhouAncientTemporaryDexterityPower
{
    public override AbstractModel OriginModel => ModelDb.Relic<YinYangOrb>();
}
