using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

/// <summary>
/// 梅蒂欣·梅兰可莉（MedicineMelancholy）
/// 称号：小小的孤独之药
/// Act 2 可选先古之民
/// </summary>
public class MedicineMelancholyAncient : CustomAncientModel
{
    public override Color ButtonColor => new(0.8f, 0.4f, 0.6f, 0.8f);
    public override Color DialogueColor => new(0.8f, 0.4f, 0.6f);

    public override string? CustomMapIconPath => "res://sprite/icon/MapNode/WatariNina_MapNode.png";
    public override string? CustomMapIconOutlinePath => "res://sprite/icon/MapNode/WatariNina_MapNode.png";
    public override string? CustomRunHistoryIconPath => "res://sprite/icon/Character/MedicineMelancholy.png";
    public override string? CustomRunHistoryIconOutlinePath => "res://sprite/icon/Character/MedicineMelancholy.png";

    public override bool IsValidForAct(ActModel act)
    {
        if (TouhouAncientsConfig.BanMedicine) return false;
        return act.ActNumber() == 2;
    }

    public override bool ShouldForceSpawn(ActModel act, AncientEventModel? rngChosenAncient)
    {
        return TouhouAncientsConfig.IsAncientForced<MedicineMelancholyAncient>(act.ActNumber());
    }

    protected override OptionPools MakeOptionPools => new OptionPools(
        MakePool(
            //AncientOption<ChildhoodBag>(),
            AncientOption<RoseCrown>(),
            AncientOption<LilyBellDiary>()
        ),
        MakePool(
            AncientOption<StageDevice>(),
            AncientOption<MaliciousFairyTale>(),
            AncientOption<MedicinePoisonBox>()
        ),
        MakePool(
            //AncientOption<HappinessElixir>(),
            AncientOption<RibbonBow>()
        )
    );
}
