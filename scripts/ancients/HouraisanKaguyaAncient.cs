using Godot;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

[RegisterSharedAncient]
public class HouraisanKaguyaAncient : TouhouAncientBase
{
    public override int? ShowAct => 3;
    public override Color ButtonColor => new(0.1f, 0.1f, 0.1f, 0.7f);
    public override Color DialogueColor => new(0.9f, 0.3f, 0.5f, 1f);

    // TODO: Restore ThatDecreasesMaxHp modifier for HotokeMishiIshiNoHachi using RitsuLib API
    public override IEnumerable<EventOption> AllPossibleOptions =>
    [
        CreateModRelicOption<KonshiiNoKusuri>(),
        CreateModRelicOption<RyukeiNoTama>(),
        CreateModRelicOption<HinezumiNoKawagoromo>(),
        CreateModRelicOption<TsubameNoKoyasugai>(),
        CreateModRelicOption<HotokeMishiIshiNoHachi>(),
        CreateModRelicOption<HouraiNoTamae>(),
        CreateModRelicOption<EienteiZakushi>(),
        CreateModRelicOption<KaguyaSecretTreasure>()
    ];
}
