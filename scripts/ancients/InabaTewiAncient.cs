using Godot;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

[RegisterSharedAncient]
public class InabaTewiAncient : TouhouAncientBase
{
    public override int? ShowAct => 2;
    public override Color ButtonColor => new(0.3f, 0.3f, 0.3f, 0.7f);
    public override Color DialogueColor => new(0.5f, 0.5f, 0.5f, 1f);

    public override IEnumerable<EventOption> AllPossibleOptions =>
    [
        CreateModRelicOption<WhiteRabbitAmulet>(),
        CreateModRelicOption<CarrotNecklace>(),
        CreateModRelicOption<FourLeafClover>(),
        CreateModRelicOption<RabbitHornContract>(),
        CreateModRelicOption<RabbitsFoot>(),
        CreateModRelicOption<LuckyTreasureChest>(),
        CreateModRelicOption<RabbitsCage>(),
        CreateModRelicOption<OokunineshiProtrayal>(),
        CreateModRelicOption<SuspiciousToken>(),
        CreateModRelicOption<GlowingBamboo>()
    ];
}