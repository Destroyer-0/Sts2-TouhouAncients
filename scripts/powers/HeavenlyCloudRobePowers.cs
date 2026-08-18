using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 天云羽衣·力量形态：打出攻击牌后转换为此形态，获得临时力量。
/// </summary>
public class HeavenlyCloudRobeStrengthPower : TouhouAncientTemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Relic<HeavenlyCloudRobe>();
}

/// <summary>
/// 天云羽衣·敏捷形态：打出技能牌后转换为此形态，获得临时敏捷。
/// </summary>
public class HeavenlyCloudRobeDexterityPower : TouhouAncientTemporaryDexterityPower
{
    public override AbstractModel OriginModel => ModelDb.Relic<HeavenlyCloudRobe>();
}