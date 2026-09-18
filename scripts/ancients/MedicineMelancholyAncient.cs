using Godot;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.encounters;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

/// <summary>
/// 梅蒂欣·梅兰可莉（MedicineMelancholy）
/// 称号：小小的孤独之药
/// Act 2 可选先古之民
/// </summary>
public class MedicineMelancholyAncient : TouhouAncientBase
{
    public override int? ShowAct => 2;
    public override Color ButtonColor => new(0.6f, 0.2f, 0.4f, 0.9f);
    public override Color DialogueColor => new(0.6f, 0.2f, 0.4f,1f);

    public override string? CustomMapIconPath => "res://images/icon/MapNode/MedicineMelancholy_MapNode.png";
    public override string? CustomMapIconOutlinePath => "res://images/icon/MapNode/Outline/MedicineMelancholy_MapNode.png";
    public override string? CustomRunHistoryIconPath => "res://images/icon/Character/MedicineMelancholy.png";
    public override string? CustomRunHistoryIconOutlinePath => "res://images/icon/Character/Outline/MedicineMelancholy.png";

    public override TouhouAncientEncounter? ChallengeEncounter => ModelDb.Encounter<MedicineMelancholyEncounter>();
    protected override IReadOnlyList<TARelicOptionGroup> TARelicOptionPools =>
    [
        CreateTARelicOptionPool(
            TARelicOption<ChildhoodBag>(),
            TARelicOption<RoseCrown>(),
            TARelicOption<LilyBellDiary>(),
            TARelicOption<SilenceDoll>()
            ),
        CreateTARelicOptionPool(
            TARelicOption<PlagueBlend>(),
            TARelicOption<MaliciousFairyTale>(),
            TARelicOption<MedicinePoisonBox>()
            ),
        CreateTARelicOptionPool(
            TARelicOption<HappinessElixir>(),
            TARelicOption<RibbonBow>(),
            TARelicOption<StageDevice>()
            )
    ];
}
