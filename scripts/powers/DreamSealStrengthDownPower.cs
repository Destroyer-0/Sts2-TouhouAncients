using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using TouhouAncients.Scripts.cards;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 梦想封印附带的临时减力量效果，回合结束时自动移除。
/// </summary>
public class DreamSealStrengthDownPower : TouhouAncientTemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Card<DreamSeal>();

    protected override bool IsPositive => false;
}
