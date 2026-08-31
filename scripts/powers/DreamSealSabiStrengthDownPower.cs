using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using TouhouAncients.Scripts.cards;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 梦想封印·寂附带的临时减力量效果，回合结束时自动移除。
/// </summary>
public class DreamSealSabiStrengthDownPower : TouhouAncientTemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Card<DreamSealSabi>();

    protected override bool IsPositive => false;

    public override string? CustomPackedIconPath => TouhouAncientCmd.CheckPathExists("res://images/icon/power/DreamSealStrengthDownPower.png");
    public override string? CustomBigIconPath => TouhouAncientCmd.CheckPathExistsWithFallback2("res://images/icon/power/BigIcon/DreamSealStrengthDownPower.png", CustomPackedIconPath);
}
