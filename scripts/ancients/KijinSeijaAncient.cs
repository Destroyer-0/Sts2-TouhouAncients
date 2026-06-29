using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

public class KijinSeijaAncient : TouhouAncientBase
{
    public override int? ShowAct => 2;
    public override Color ButtonColor => new(0.5f, 0.173f, 0.165f, 0.6f);
    public override Color DialogueColor => new(0.588f, 0.173f, 0.165f, 1f);

    public override string? CustomMapIconPath => "res://images/icon/MapNode/KijinSeija_MapNode.png";
    public override string? CustomMapIconOutlinePath => "res://images/icon/MapNode/Outline/KijinSeija_MapNode.png";

    public override string? CustomRunHistoryIconPath => "res://images/icon/Character/KijinSeija.png";
    public override string? CustomRunHistoryIconOutlinePath => "res://images/icon/Character/Outline/KijinSeija.png";

    protected override OptionPools MakeOptionPools => new OptionPools(
        MakePool(
            AncientOption<InvisibilityCloth>(),
            AncientOption<BloodYinYangOrb>(),
            AncientOption<RebellionHorn>(),
            AncientOption<BatteryBili>(),
            AncientOption<GhostLantern>(),
            AncientOption<MagicMallet>(),
            AncientOption<FoldingUmbrella>(),
            AncientOption<FakeSpiritOrb>(),
            AncientOption<HungryBackpack>(),
            AncientOption<DreamHeavenBow>()
        )
    );
}
