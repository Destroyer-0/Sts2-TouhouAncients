using Godot;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

[RegisterSharedAncient]
public class HinanawiTenshiAncient : TouhouAncientBase
{
    public override int? ShowAct => 3;
    public override Color ButtonColor => new(0.0f, 0.4f, 0.8f, 0.6f);
    public override Color DialogueColor => new(0.0f, 0.63f, 1f, 1f);

    // TODO: Pool 2 had dynamic weights (Regent check). Currently flattened.
    public override IEnumerable<EventOption> AllPossibleOptions =>
    [
        CreateModRelicOption<MysticFortunePeach>(),
        CreateModRelicOption<HolyArmor>(),
        CreateModRelicOption<HeavenlyRevelation>(),
        CreateModRelicOption<HisouSword>(),
        CreateModRelicOption<KeystoneFloatingCannon>(),
        CreateModRelicOption<FirmamentSash>(),
        CreateModRelicOption<CurseBreakerQi>(),
        CreateModRelicOption<CelestialIndifference>(),
        CreateModRelicOption<CosmicDecree>()
    ];
}
