using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Events;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

[RegisterSharedAncient]
public class RemiliaScarletAncient : TouhouAncientBase
{
    public override int? ShowAct => 3;
    public override Color ButtonColor => new(0.6f, 0.1f, 0.1f, 0.5f);
    public override Color DialogueColor => new(0.6f, 0.1f, 0.1f, 1f);



    public override IEnumerable<EventOption> AllPossibleOptions =>
    [
        CreateModRelicOption<NobleBrooch>(),
        CreateModRelicOption<CrimsonCrystal>(),
        CreateModRelicOption<NightServant>(),
        CreateModRelicOption<CrimsonChalice>(),
        CreateModRelicOption<BloodFang>(),
        CreateModRelicOption<PreservedRedFog>(),
        CreateModRelicOption<DraculaLegacy>(),
        CreateModRelicOption<LordsSunscreenCream>()
    ];
}
