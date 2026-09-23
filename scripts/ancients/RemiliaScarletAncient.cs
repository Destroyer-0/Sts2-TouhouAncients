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
            // 殷红之牙的事件描述要实时显示「会失去的最大生命值」，必须在事件界面渲染前就填好，
            // 因此用 Prep 预准备；此阶段遗物自身的 Owner 尚未设置，用古代事件自己的 Owner。
            TARelicOption<BloodFang>().Prep((BloodFang relic) =>
            {
                if (Owner != null) relic.SetupForPlayer(Owner);
            }),
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
