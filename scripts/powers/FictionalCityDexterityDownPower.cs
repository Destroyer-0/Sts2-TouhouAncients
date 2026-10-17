using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.cards;

namespace TouhouAncients.Scripts.powers;

/// <summary>虚构都市：本回合临时降低敏捷。</summary>
public sealed class FictionalCityDexterityDownPower : TouhouAncientTemporaryDexterityPower
{
    public override AbstractModel OriginModel => ModelDb.Card<FictionalCity>();

    protected override bool IsPositive => false;
}
