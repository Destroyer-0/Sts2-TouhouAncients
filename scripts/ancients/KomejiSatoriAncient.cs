using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Models;
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

    protected override OptionPools MakeOptionPools => new OptionPools(
        MakePool(
            AncientOption<TheThirdEye>()
        ),
        MakePool(
            AncientOption<HellOrin>(),
            AncientOption<HellOkuu>(),
            AncientOption<DustyRose>()
        ),
        MakePool(
            AncientOption<MindProbe>(),
            AncientOption<MemoryFlask>(),
            AncientOption<BrainInAVat>(),
            AncientOption<OblivionFragment>(),
            AncientOption<DetectiveStory>(),
            AncientOption<BitterCoffee>()
        ));

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        List<EventOption> list = Pool1.ToList();
        List<EventOption> list2 = Pool2.ToList();
        List<EventOption> list3 = Pool3.ToList();
        list.UnstableShuffle(base.Rng);
        list2.UnstableShuffle(base.Rng);
        list3.UnstableShuffle(base.Rng);
        return
        [
            RelicOption<TheThirdEye>(),
            list[0],
            list2[0],
            list3[0]
        ];
    }

    protected override IEnumerable<EventOption> GetAncientOptions() => Pool0.Concat(Pool1).Concat(Pool2).Concat(Pool3);

    private IEnumerable<EventOption> Pool0 =>
    [
        RelicOption<TheThirdEye>(),
    ];
    private IEnumerable<EventOption> Pool1 =>
    [
        RelicOption<HellOrin>(),
        RelicOption<HellOkuu>(),
        RelicOption<DustyRose>()
    ];

    private IEnumerable<EventOption> Pool2 =>
    [
        RelicOption<MindProbe>(),
        RelicOption<DetectiveStory>(),
        RelicOption<BitterCoffee>(),
    ];

    private IEnumerable<EventOption> Pool3 =>
    [
        RelicOption<OblivionFragment>(),
        RelicOption<BrainInAVat>(),
        RelicOption<MemoryFlask>()
    ];
}