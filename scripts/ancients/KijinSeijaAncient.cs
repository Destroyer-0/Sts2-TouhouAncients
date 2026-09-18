using Godot;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

public class KijinSeijaAncient : TouhouAncientBase
{
    public override int? ShowAct => 2;
    public override Color ButtonColor => new(0.5f, 0.173f, 0.165f, 0.6f);
    public override Color DialogueColor => new(0.588f, 0.173f, 0.165f, 1f);

    public override string? CustomMapIconPath => "res://images/icon/MapNode/KijinSeija_MapNode.png";
    public override string? CustomMapIconOutlinePath => "res://images/icon/MapNode/Outline/KijinSeija_MapNode.png";

    public override string? CustomRunHistoryIconPath => "res://images/icon/Character/KijinSeija.png";
    public override string? CustomRunHistoryIconOutlinePath => "res://images/icon/Character/Outline/KijinSeija.png";

    /// <summary>三个选项共用同一个池（池内不重复）。</summary>
    protected override IReadOnlyList<TARelicOptionGroup> TARelicOptionPools =>
    [
        (MainPool, 3)
    ];

    private TARelicOptionPool MainPool => CreateTARelicOptionPool(
        TARelicOption<InvisibilityCloth>(),
        TARelicOption<BloodYinYangOrb>(),
        TARelicOption<RebellionHorn>(),
        TARelicOption<BatteryBili>(),
        TARelicOption<GhostLantern>(),
        TARelicOption<MagicMallet>(),
        TARelicOption<FoldingUmbrella>(),
        TARelicOption<FakeSpiritOrb>(),
        TARelicOption<HungryBackpack>(),
        TARelicOption<DreamHeavenBow>()
        );
}
