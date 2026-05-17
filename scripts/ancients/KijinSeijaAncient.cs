using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

public class KijinSeijaAncient : CustomAncientModel
{
    public override Color ButtonColor => new(0.588f, 0.173f, 0.165f, 0.8f);
    public override Color DialogueColor => new(0.588f, 0.173f, 0.165f);

    public override string? CustomMapIconPath => "res://sprite/icon/MapNode/WatariNina_MapNode.png";
    public override string? CustomMapIconOutlinePath => "res://sprite/icon/MapNode/WatariNina_MapNode.png";

    public override string? CustomRunHistoryIconPath => "res://sprite/icon/Character/KijinSeija.png";
    public override string? CustomRunHistoryIconOutlinePath => "res://sprite/icon/Character/Outline/KijinSeija.png";

    public override bool IsValidForAct(ActModel act)
    {
        if (TouhouAncientsConfig.BanSeija) return false;
        return act.ActNumber() == 2;
    }

    public override bool ShouldForceSpawn(ActModel act, AncientEventModel? rngChosenAncient)
    {
        return TouhouAncientsConfig.IsAncientForced<KijinSeijaAncient>(act.ActNumber());
    }

    protected override OptionPools MakeOptionPools => new OptionPools(
        MakePool(
            AncientOption<InvisibilityCloth>(),
            AncientOption<BloodYinYangOrb>(),
            AncientOption<RebellionHorn>(),
            AncientOption<BatteryBili>(),
            AncientOption<GhostLantern>(),
            AncientOption<MagicMallet>(),
            AncientOption<FoldingUmbrella>(),
            AncientOption<FakeSpiritOrb>()
        )
    );
}
