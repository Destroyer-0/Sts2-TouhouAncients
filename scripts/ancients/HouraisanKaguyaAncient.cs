using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

public class HouraisanKaguyaAncient : CustomAncientModel
{
    public override Color ButtonColor => new(0.9f, 0.3f, 0.5f, 0.5f);
    public override Color DialogueColor => new(0.9f, 0.3f, 0.5f, 1f);

    public override string? CustomMapIconPath => "res://images/icon/MapNode/WatariNina_MapNode.png";
    public override string? CustomMapIconOutlinePath => "res://images/icon/MapNode/WatariNina_MapNode.png";
    public override string? CustomRunHistoryIconPath => "res://images/icon/Character/HouraisanKaguya.png";
    public override string? CustomRunHistoryIconOutlinePath => "res://images/icon/Character/Outline/HouraisanKaguya.png";

    public override bool IsValidForAct(ActModel act)
    {
        return act.ActNumber() == 3;
    }

    public override bool ShouldForceSpawn(ActModel act, AncientEventModel? rngChosenAncient)
    {
        return TouhouAncientsConfig.IsAncientForced<HouraisanKaguyaAncient>(act.ActNumber());
    }

    protected override OptionPools MakeOptionPools => new OptionPools(
        MakePool(
            AncientOption<KonshiiNoKusuri>(),
            AncientOption<RyukeiNoTama>(),
            AncientOption<HinezumiNoKawagoromo>(),
            AncientOption<TsubameNoKoyasugai>(),
            AncientOption<HotokeMishiIshiNoHachi>(),
            AncientOption<HouraiNoTamae>(),
            AncientOption<EienteiZakushi>(),
            AncientOption<KaguyaSecretTreasure>()
        ));
}
