using Godot;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

public class InabaTewiAncient : TouhouAncientBase
{
    public override int? ShowAct => 2;
    public override Color ButtonColor => new(0.3f, 0.3f, 0.3f, 0.7f);
    public override Color DialogueColor => new(0.5f, 0.5f, 0.5f, 1f);

    public override string? CustomMapIconPath => "res://images/icon/MapNode/InabaTewi_MapNode.png";
    public override string? CustomMapIconOutlinePath => "res://images/icon/MapNode/Outline/InabaTewi_MapNode.png";
    public override string? CustomRunHistoryIconPath => "res://images/icon/Character/InabaTewi.png";
    public override string? CustomRunHistoryIconOutlinePath => "res://images/icon/Character/Outline/InabaTewi.png";

    /// <summary>三个选项共用同一个池（池内不重复）。</summary>
    protected override IReadOnlyList<TARelicOptionGroup> TARelicOptionPools =>
    [
        (MainPool, 3)
    ];

    private TARelicOptionPool MainPool => CreateTARelicOptionPool(
        TARelicOption<WhiteRabbitAmulet>(),
        TARelicOption<CarrotNecklace>(),
        TARelicOption<FourLeafClover>(),
        TARelicOption<RabbitHornContract>(),
        TARelicOption<RabbitsFoot>(),
        TARelicOption<LuckyTreasureChest>(),
        TARelicOption<RabbitsCage>(),
        TARelicOption<OokunineshiProtrayal>(),
        TARelicOption<SuspiciousToken>(),
        TARelicOption<GlowingBamboo>()
        );
}
