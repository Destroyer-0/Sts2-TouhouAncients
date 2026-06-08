using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

public class JunkoAncient : CustomAncientModel
{
    public override Color ButtonColor => new(0.5f, 0.0f, 1f, 0.7f);
    public override Color DialogueColor => new(0.5f, 0.0f, 1f, 1f);

    public override string? CustomMapIconPath => "res://images/icon/MapNode/WatariNina_MapNode.png";
    public override string? CustomMapIconOutlinePath => "res://images/icon/MapNode/WatariNina_MapNode.png";
    public override string? CustomRunHistoryIconPath => "res://images/icon/Character/KomejiSatori.png";
    public override string? CustomRunHistoryIconOutlinePath => "res://images/icon/Character/Outline/KomejiSatori.png";

    public override bool IsValidForAct(ActModel act)
    {
        return act.ActNumber() == 3;
    }

    public override bool ShouldForceSpawn(ActModel act, AncientEventModel? rngChosenAncient)
    {
        return TouhouAncientsConfig.IsAncientForced<JunkoAncient>(act.ActNumber());
    }

    protected override OptionPools MakeOptionPools => new OptionPools(
        MakePool(
            AncientOption<CeaselessResentment>(),
            AncientOption<DeathBlackCrown>(),
            AncientOption<OverflowingDefilement>()
        ),
        MakePool(
            AncientOption<MurderousLily>(),
            AncientOption<IllusoryProjection>()
        ),
        MakePool(
            AncientOption<HellOfBullets>(),
            AncientOption<TremblingFrozenStar>()
        )
    );
}
