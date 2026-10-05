using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 亡灵雷云：本回合获得的临时力量。
/// </summary>
public class GhostThunderCloudStrengthPower : TouhouAncientTemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Relic<GhostThunderCloud>();
}
