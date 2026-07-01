using Godot;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

[RegisterSharedAncient]
public class KomejiSatoriAncient : TouhouAncientBase
{
    public override int? ShowAct => 2;
    public override Color ButtonColor => new(0.75f, 0.25f, 0.55f, 0.7f);
    public override Color DialogueColor => new(0.85f, 0.35f, 0.75f, 1f);


    // Each pool is randomly picked, with TheThirdEye always offered.
    public override IEnumerable<EventOption> AllPossibleOptions =>
    [
        CreateModRelicOption<TheThirdEye>(),
        CreateModRelicOption<HellOrin>(),
        CreateModRelicOption<HellOkuu>(),
        CreateModRelicOption<DustyRose>(),
        CreateModRelicOption<MindProbe>(),
        CreateModRelicOption<DetectiveStory>(),
        CreateModRelicOption<BitterCoffee>(),
        CreateModRelicOption<OblivionFragment>(),
        CreateModRelicOption<BrainInAVat>(),
        CreateModRelicOption<MemoryFlask>()
    ];
}