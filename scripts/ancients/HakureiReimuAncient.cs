using Godot;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.encounters;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

/// <summary>
/// 先古之民：博丽灵梦（Hakurei Reimu）
/// 红白巫女，博丽神社的现任巫女，解决异变的专家。
/// </summary>
public class HakureiReimuAncient : TouhouAncientBase
{
    public override int? ShowAct => 2;
    public override Color ButtonColor => new(0.9f, 0.25f, 0.25f, 0.65f);
    public override Color DialogueColor => new(0.9f, 0.25f, 0.25f, 1f);

    public override string? CustomMapIconPath => "res://images/icon/MapNode/HakureiReimu_MapNode.png";
    public override string? CustomMapIconOutlinePath => "res://images/icon/MapNode/Outline/HakureiReimu_MapNode.png";
    public override string? CustomRunHistoryIconPath => "res://images/icon/Character/HakureiReimu.png";
    public override string? CustomRunHistoryIconOutlinePath => "res://images/icon/Character/Outline/HakureiReimu.png";

    public override TouhouAncientEncounter? ChallengeEncounter => ModelDb.Encounter<HakureiReimuEncounter>();

    protected override IReadOnlyList<TARelicOptionGroup> TARelicOptionPools =>
    [
        CreateTARelicOptionPool(
            TARelicOption<HakureiGohei>(),
            TARelicOption<SubspaceHole>(),
            TARelicOption<HakureiAmulet>()
            ),
        CreateTARelicOptionPool(
            TARelicOption<MiniShrine>(),
            TARelicOption<DonateMoneyBox>()
            ),
        CreateTARelicOptionPool(
            TARelicOption<YinYangOrb>(),
            TARelicOption<SealingNeedle>(),
            TARelicOption<DuplexBarrier>()
            )
    ];
}
