using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

public class RemiliaScarletAncient : CustomAncientModel
{
    public override Color ButtonColor => new(0.6f, 0.1f, 0.1f, 0.7f);
    public override Color DialogueColor => new(0.6f, 0.1f, 0.1f);

    public override bool IsValidForAct(ActModel act)
    {
        if (TouhouAncientsConfig.BanRemilia) return false;
        return act.ActNumber() == 3;
    }

    public override bool ShouldForceSpawn(ActModel act, AncientEventModel? rngChosenAncient)
    {
        return TouhouAncientsConfig.IsAncientForced<RemiliaScarletAncient>()&& act.ActNumber() == 3;
    }
    
    public override string? CustomMapIconPath => "res://sprite/icon/WatariNina_MapNode.png";

    public override string? CustomMapIconOutlinePath => "res://sprite/icon/WatariNina_MapNode.png";

    // 历史记录图标路径
    public override string? CustomRunHistoryIconPath => "res://sprite/icon/RemiliaScarlet.png";
    public override string? CustomRunHistoryIconOutlinePath => "res://sprite/icon/RemiliaScarlet.png";

    protected override OptionPools MakeOptionPools => new OptionPools(
        MakePool(
            AncientOption<DraculaLegacy>(),
            AncientOption<NobleBrooch>()
        ),
        MakePool(
            AncientOption<CrimsonChalice>(),
            AncientOption<BloodFang>(),
            AncientOption<PreservedRedFog>()
        ),
        MakePool(
            AncientOption<CrimsonChalice>(),
            AncientOption<BloodFang>(),
            AncientOption<PreservedRedFog>()
        )
        // MakePool(
        //     AncientOption<SpearGungnir>(),
        //     AncientOption<NightServant>()
        // )
        );
}
