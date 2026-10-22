using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 独眼唐伞：敌人本回合失去的力量，其回合结束时自动回补。
/// </summary>
public class OneEyedKarakasaStrengthDownPower : TouhouAncientTemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Relic<OneEyedKarakasa>();

    protected override bool IsPositive => false;
}
