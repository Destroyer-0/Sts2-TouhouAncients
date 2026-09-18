using Godot;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

public class KomejiSatoriAncient : TouhouAncientBase
{
    public override int? ShowAct => 2;
    public override Color ButtonColor => new(0.75f, 0.25f, 0.55f, 0.7f);
    public override Color DialogueColor => new(0.85f, 0.35f, 0.75f, 1f);


    public override string? CustomMapIconPath => "res://images/icon/MapNode/KomejiSatori_MapNode.png";
    public override string? CustomMapIconOutlinePath => "res://images/icon/MapNode/Outline/KomejiSatori_MapNode.png";
    public override string? CustomRunHistoryIconPath => "res://images/icon/Character/KomejiSatori.png";
    public override string? CustomRunHistoryIconOutlinePath => "res://images/icon/Character/Outline/KomejiSatori.png";

    /// <summary>本 Ancient 的选项（四行 = 四个选项）。</summary>
    protected override IReadOnlyList<TARelicOptionGroup> TARelicOptionPools =>
    [
        CreateTARelicOptionPool(
            TARelicOption<TheThirdEye>()
            ),
        CreateTARelicOptionPool(
            TARelicOption<HellOrin>(),
            TARelicOption<HellOkuu>(),
            TARelicOption<DustyRose>()
            ),
        CreateTARelicOptionPool(
            TARelicOption<MindProbe>(),
            TARelicOption<DetectiveStory>(),
            TARelicOption<BitterCoffee>()
            ),
        CreateTARelicOptionPool(
            TARelicOption<OblivionFragment>(),
            TARelicOption<BrainInAVat>(),
            TARelicOption<MemoryFlask>()
            )
    ];
}