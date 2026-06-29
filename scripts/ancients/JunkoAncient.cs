using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

public class JunkoAncient : TouhouAncientBase
{
    public override int? ShowAct => 3;
    public override Color ButtonColor => new(0.5f, 0.0f, 1f, 0.7f);
    public override Color DialogueColor => new(0.5f, 0.0f, 1f, 1f);

    public override string? CustomMapIconPath => "res://images/icon/MapNode/Junko_MapNode.png";
    public override string? CustomMapIconOutlinePath => "res://images/icon/MapNode/Outline/Junko_MapNode.png";
    public override string? CustomRunHistoryIconPath => "res://images/icon/Character/Junko.png";
    public override string? CustomRunHistoryIconOutlinePath => "res://images/icon/Character/Outline/Junko.png";

    protected override OptionPools MakeOptionPools => new OptionPools(
        MakePool(
            AncientOption<CeaselessResentment>(),
            AncientOption<DeathBlackCrown>(),
            AncientOption<TremblingFrozenStar>()
        ),
        MakePool(
            AncientOption<MurderousLily>(),
            AncientOption<HellOfBullets>(),
            AncientOption<PureConfidence>()
        ),
        MakePool(
            AncientOption<IllusoryProjection>(),
            AncientOption<PrimalSpirit>(),
            AncientOption<OverflowingDefilement>()
        )
    );
}
