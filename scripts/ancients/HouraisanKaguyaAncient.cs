using Godot;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.encounters;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

public class HouraisanKaguyaAncient : TouhouAncientBase
{
    public override int? ShowAct => 3;
    public override Color ButtonColor => new(0.1f, 0.1f, 0.1f, 0.7f);
    public override Color DialogueColor => new(0.9f, 0.3f, 0.5f, 1f);

    public override string? CustomMapIconPath => "res://images/icon/MapNode/HouraisanKaguya_MapNode.png";
    public override string? CustomMapIconOutlinePath => "res://images/icon/MapNode/Outline/HouraisanKaguya_MapNode.png";
    public override string? CustomRunHistoryIconPath => "res://images/icon/Character/HouraisanKaguya.png";
    public override string? CustomRunHistoryIconOutlinePath => "res://images/icon/Character/Outline/HouraisanKaguya.png";

    public override TouhouAncientEncounter? ChallengeEncounter => ModelDb.Encounter<HouraisanKaguyaEncounter>();
    
    protected override IReadOnlyList<TARelicOptionGroup> TARelicOptionPools =>
    [
        (CreateTARelicOptionPool(
            TARelicOption<RyukeiNoTama>(),
            TARelicOption<HinezumiNoKawagoromo>(),
            TARelicOption<TsubameNoKoyasugai>(),
            TARelicOption<HotokeMishiIshiNoHachi>(),
            TARelicOption<HouraiNoTamae>()
        ), 2),
        CreateTARelicOptionPool(
            TARelicOption<KonshiiNoKusuri>(),
            TARelicOption<EienteiZakushi>(),
            TARelicOption<KaguyaSecretTreasure>()
        )
    ];
}
