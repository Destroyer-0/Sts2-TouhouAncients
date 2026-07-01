using Godot;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

[RegisterSharedAncient]
public class JunkoAncient : TouhouAncientBase
{
    public override int? ShowAct => 3;
    public override Color ButtonColor => new(0.5f, 0.0f, 1f, 0.7f);
    public override Color DialogueColor => new(0.5f, 0.0f, 1f, 1f);

    public override IEnumerable<EventOption> AllPossibleOptions =>
    [
        CreateModRelicOption<CeaselessResentment>(),
        CreateModRelicOption<DeathBlackCrown>(),
        CreateModRelicOption<TremblingFrozenStar>(),
        CreateModRelicOption<MurderousLily>(),
        CreateModRelicOption<HellOfBullets>(),
        CreateModRelicOption<PureConfidence>(),
        CreateModRelicOption<IllusoryProjection>(),
        CreateModRelicOption<PrimalSpirit>(),
        CreateModRelicOption<OverflowingDefilement>()
    ];
}
