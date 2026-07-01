using Godot;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

[RegisterSharedAncient]
public class KijinSeijaAncient : TouhouAncientBase
{
    public override int? ShowAct => 2;
    public override Color ButtonColor => new(0.5f, 0.173f, 0.165f, 0.6f);
    public override Color DialogueColor => new(0.588f, 0.173f, 0.165f, 1f);

    public override IEnumerable<EventOption> AllPossibleOptions =>
    [
        CreateModRelicOption<InvisibilityCloth>(),
        CreateModRelicOption<BloodYinYangOrb>(),
        CreateModRelicOption<RebellionHorn>(),
        CreateModRelicOption<BatteryBili>(),
        CreateModRelicOption<GhostLantern>(),
        CreateModRelicOption<MagicMallet>(),
        CreateModRelicOption<FoldingUmbrella>(),
        CreateModRelicOption<FakeSpiritOrb>(),
        CreateModRelicOption<HungryBackpack>(),
        CreateModRelicOption<DreamHeavenBow>()
    ];
}
