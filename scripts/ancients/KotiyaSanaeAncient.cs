using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Events;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

[RegisterSharedAncient]
public class KotiyaSanaeAncient : TouhouAncientBase
{
    public override int? ShowAct => null;
    public override Color ButtonColor => new(0.1f, 0.5f, 0.2078f, 0.7f);
    public override Color DialogueColor => new(0.2275f, 0.6157f, 0.2078f, 1f);




    public override IEnumerable<EventOption> AllPossibleOptions =>
    [
        CreateModRelicOption<MoriyaGohei>(),
        CreateModRelicOption<DayKakusei>(),
        CreateModRelicOption<WindPriestessWine>(),
        CreateModRelicOption<SnakeAmulet>(),
        CreateModRelicOption<FrogAmulet>(),
        CreateModRelicOption<HisoutensokuModel>(),
        CreateModRelicOption<SailorSuit>(),
        CreateModRelicOption<GiftFromMountain>(),
        CreateModRelicOption<MiracleNoble>()
    ];
}