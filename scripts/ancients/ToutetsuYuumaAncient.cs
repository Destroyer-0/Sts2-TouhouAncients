using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

public class ToutetsuYuumaAncient : CustomAncientModel
{
    public override Color ButtonColor => new(0.6f, 0.1f, 0.1f, 0.7f);
    public override Color DialogueColor => new(0.9f, 0.2f, 0.2f, 1f);

    public override string? CustomMapIconPath => "res://images/icon/MapNode/WatariNina_MapNode.png";
    public override string? CustomMapIconOutlinePath => "res://images/icon/MapNode/WatariNina_MapNode.png";
    public override string? CustomRunHistoryIconPath => "res://images/icon/Character/ToutetsuYuuma.png";
    public override string? CustomRunHistoryIconOutlinePath => "res://images/icon/Character/Outline/ToutetsuYuuma.png";

    public override bool IsValidForAct(ActModel act)
    {
        return act.ActNumber() == 3;
    }

    public override bool ShouldForceSpawn(ActModel act, AncientEventModel? rngChosenAncient)
    {
        return TouhouAncientsConfig.IsAncientForced<ToutetsuYuumaAncient>(act.ActNumber());
    }

    protected override OptionPools MakeOptionPools => new OptionPools(
        MakePool(
            AncientOption<BottomlessStomach>(),
            AncientOption<GluttonousFang>(),
            AncientOption<BloodlickingTongue>(),
            AncientOption<SkySwallowingSpoon>(),
            AncientOption<RigidDesireProof>(),
            AncientOption<GreedyEye>(),
            AncientOption<CursedBlood>(),
            AncientOption<PurgatoryEmbers>(),
            AncientOption<GuiltlessFace>(),
            AncientOption<EstrangedHeart>()
        ));
}
