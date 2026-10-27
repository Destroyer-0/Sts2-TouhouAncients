using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.relics.IdleKoishi;

namespace TouhouAncients.Scripts.powers;

/// <summary>无意识麦克风：敌人本回合额外失去的力量。</summary>
public sealed class UnconsciousMicStrengthDownPower : TouhouAncientTemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Relic<UnconsciousMicrophone>();

    protected override bool IsPositive => false;
}
