using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

public class InabaTewiAncient : TouhouAncientBase
{
    public override int? ShowAct => 2;
    public override Color ButtonColor => new(0.5f, 0.5f, 0.5f, 0.7f);
    public override Color DialogueColor => new(0.5f, 0.5f, 0.5f, 1f);

    public override string? CustomMapIconPath => "res://images/icon/MapNode/InabaTewi_MapNode.png";
    public override string? CustomMapIconOutlinePath => "res://images/icon/MapNode/Outline/InabaTewi_MapNode.png";
    public override string? CustomRunHistoryIconPath => "res://images/icon/Character/InabaTewi.png";
    public override string? CustomRunHistoryIconOutlinePath => "res://images/icon/Character/Outline/InabaTewi.png";

    protected override OptionPools MakeOptionPools => new OptionPools(
        MakePool(
            AncientOption<WhiteRabbitAmulet>(),
            AncientOption<CarrotNecklace>(),
            AncientOption<FourLeafClover>(),
            AncientOption<RabbitHornContract>(),
            AncientOption<RabbitsFoot>(),
            AncientOption<LuckyTreasureChest>(),
            AncientOption<RabbitsCage>(),
            AncientOption<OokunineshiProtrayal>()
        ));
}
