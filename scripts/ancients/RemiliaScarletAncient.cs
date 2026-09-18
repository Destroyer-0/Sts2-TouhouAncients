using Godot;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

public class RemiliaScarletAncient : TouhouAncientBase
{
    public override int? ShowAct => 3;
    public override Color ButtonColor => new(0.6f, 0.1f, 0.1f, 0.5f);
    public override Color DialogueColor => new(0.6f, 0.1f, 0.1f, 1f);

    public override string? CustomMapIconPath => "res://images/icon/MapNode/RemiliaScarlet_MapNode.png";

    public override string? CustomMapIconOutlinePath => "res://images/icon/MapNode/Outline/RemiliaScarlet_MapNode.png";

    // 历史记录图标路径
    public override string? CustomRunHistoryIconPath => "res://images/icon/Character/RemiliaScarlet.png";
    public override string? CustomRunHistoryIconOutlinePath => "res://images/icon/Character/Outline/RemiliaScarlet.png";

    protected override IReadOnlyList<TARelicOptionGroup> TARelicOptionPools =>
    [
        CreateTARelicOptionPool(
            TARelicOption<NobleBrooch>(),
            TARelicOption<CrimsonCrystal>(),
            TARelicOption<NightServant>()
            ),
        CreateTARelicOptionPool(
            TARelicOption<CrimsonChalice>(),
            TARelicOption<BloodFang>(),
            TARelicOption<PreservedRedFog>()
            ),
        CreateTARelicOptionPool(
            TARelicOption<DraculaLegacy>(),
            TARelicOption<SpearGungnir>(),
            TARelicOption<LordsSunscreenCream>()
            )
        // TARelicOptionPool(TARelicOption<SpearGungnir>(), TARelicOption<NightServant>())
    ];
}
