using Godot;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

public class KotiyaSanaeAncient : TouhouAncientBase
{
    public override int? ShowAct => null;
    public override Color ButtonColor => new(0.1f, 0.5f, 0.2078f, 0.7f);
    public override Color DialogueColor => new(0.2275f, 0.6157f, 0.2078f, 1f);

    public override string? CustomMapIconPath => "res://images/icon/MapNode/KotiyaSanae_MapNode.png";

    public override string? CustomMapIconOutlinePath => "res://images/icon/MapNode/Outline/KotiyaSanae_MapNode.png";

    // 历史记录图标路径
    public override string? CustomRunHistoryIconPath => "res://images/icon/Character/KotiyaSanae.png";
    public override string? CustomRunHistoryIconOutlinePath => "res://images/icon/Character/Outline/KotiyaSanae.png";

    protected override IReadOnlyList<TARelicOptionGroup> TARelicOptionPools =>
    [
        CreateTARelicOptionPool(
            TARelicOption<MoriyaGohei>(),
            TARelicOption<DayKakusei>(),
            TARelicOption<WindPriestessWine>()
            ),
        CreateTARelicOptionPool(
            TARelicOption<SnakeAmulet>(),
            TARelicOption<FrogAmulet>(),
            TARelicOption<HisoutensokuModel>()
            ),
        CreateTARelicOptionPool(
            TARelicOption<SailorSuit>(),
            TARelicOption<GiftFromMountain>(),
            TARelicOption<MiracleNoble>()
            )
    ];
}