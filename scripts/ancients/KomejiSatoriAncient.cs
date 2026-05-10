using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

public class KomejiSatoriAncient : CustomAncientModel
{
    public override Color ButtonColor => new(0.85f, 0.35f, 0.75f, 0.7f);
    public override Color DialogueColor => new(0.85f, 0.35f, 0.75f);

    public override bool IsValidForAct(ActModel act) => act.ActNumber() == 2;

   // public override bool ShouldForceSpawn(ActModel act, AncientEventModel? rngChosenAncient) => act.ActNumber() == 2;

    public override string? CustomMapIconPath => "res://sprite/icon/WatariNina_MapNode.png";
    public override string? CustomMapIconOutlinePath => "res://sprite/icon/WatariNina_MapNode.png";
    public override string? CustomRunHistoryIconPath => "res://sprite/icon/KotiyaSanae.png";
    public override string? CustomRunHistoryIconOutlinePath => "res://sprite/icon/KotiyaSanae.png";

    protected override OptionPools MakeOptionPools => new OptionPools(
        MakePool(
            AncientOption<TheThirdEye>()
        ),
        MakePool(
            AncientOption<HellOrin>(),
            AncientOption<HellOkuu>()
        ),
        MakePool(
            AncientOption<MindProbe>(),
            AncientOption<MemoryFlask>(),
            AncientOption<BrainInAVat>(),
            AncientOption<OblivionFragment>()
        ));
}
