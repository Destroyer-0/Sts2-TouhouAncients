using Godot;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

/// <summary>

[RegisterSharedAncient]
public class HakureiReimuAncient : TouhouAncientBase
{
    public override int? ShowAct => 2;
    public override Color ButtonColor => new(0.9f, 0.25f, 0.25f, 0.5f);
    public override Color DialogueColor => new(0.9f, 0.25f, 0.25f, 1f);

    public override IEnumerable<EventOption> AllPossibleOptions =>
    [
        CreateModRelicOption<HakureiGohei>(),
        CreateModRelicOption<SubspaceHole>(),
        CreateModRelicOption<HakureiAmulet>(),
        CreateModRelicOption<MiniShrine>(),
        CreateModRelicOption<DonateMoneyBox>(),
        CreateModRelicOption<YinYangOrb>(),
        CreateModRelicOption<SealingNeedle>(),
        CreateModRelicOption<DuplexBarrier>()
    ];

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        throw new NotImplementedException();
    }
}
