using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.relics.Akyuu;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 侦探小说手稿：下回合获得的临时敏捷。
/// </summary>
public class DetectiveManuscriptDexterityPower : TouhouAncientTemporaryDexterityPower
{
    public override AbstractModel OriginModel => ModelDb.Relic<DetectiveManuscript>();
}
