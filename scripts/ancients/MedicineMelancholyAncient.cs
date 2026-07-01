using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Events;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

/// <summary>
/// 梅蒂欣·梅兰可莉（MedicineMelancholy�?/// 称号：小小的孤独之药
/// Act 2 可选先古之�?/// </summary>
[RegisterSharedAncient]
public class MedicineMelancholyAncient : TouhouAncientBase
{
    public override int? ShowAct => 2;
    public override Color ButtonColor => new(0.6f, 0.2f, 0.4f, 0.9f);
    public override Color DialogueColor => new(0.6f, 0.2f, 0.4f,1f);


    public override IEnumerable<EventOption> AllPossibleOptions =>
    [
        CreateModRelicOption<ChildhoodBag>(),
        CreateModRelicOption<RoseCrown>(),
        CreateModRelicOption<LilyBellDiary>(),
        CreateModRelicOption<SilenceDoll>(),
        CreateModRelicOption<PlagueBlend>(),
        CreateModRelicOption<MaliciousFairyTale>(),
        CreateModRelicOption<MedicinePoisonBox>(),
        CreateModRelicOption<HappinessElixir>(),
        CreateModRelicOption<RibbonBow>(),
        CreateModRelicOption<StageDevice>()
    ];
}

