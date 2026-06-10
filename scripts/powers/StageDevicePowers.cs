using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 舞台装置·奇数回合：获得的临时力量。
/// </summary>
public class StageDeviceStrengthPower : TouhouAncientTemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Relic<StageDevice>();
}

/// <summary>
/// 舞台装置·偶数回合：获得的临时敏捷。
/// </summary>
public class StageDeviceDexterityPower : TouhouAncientTemporaryDexterityPower
{
    public override AbstractModel OriginModel => ModelDb.Relic<StageDevice>();
}
