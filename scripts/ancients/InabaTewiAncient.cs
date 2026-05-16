using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

public class InabaTewiAncient : CustomAncientModel
{
    public override Color ButtonColor => new(0.5f, 0.5f, 0.5f, 0.8f);
    public override Color DialogueColor => new(0.5f, 0.5f, 0.5f);

    public override string? CustomMapIconPath => "res://sprite/icon/WatariNina_MapNode.png";
    public override string? CustomMapIconOutlinePath => "res://sprite/icon/WatariNina_MapNode.png";
    public override string? CustomRunHistoryIconPath => "res://sprite/icon/InabaTewi.png";
    public override string? CustomRunHistoryIconOutlinePath => "res://sprite/icon/InabaTewi.png";

    public override bool IsValidForAct(ActModel act)
    {
        if (TouhouAncientsConfig.BanTewi) return false;
        return act.ActNumber() == 2;
    }

    public override bool ShouldForceSpawn(ActModel act, AncientEventModel? rngChosenAncient)
    {
        return TouhouAncientsConfig.IsAncientForced<InabaTewiAncient>(act.ActNumber());
    }

    protected override OptionPools MakeOptionPools => new OptionPools(
        MakePool(
            AncientOption<WhiteRabbitAmulet>(),
            AncientOption<CarrotNecklace>(),
            AncientOption<FourLeafClover>(),
            AncientOption<RabbitHornContract>(),
            AncientOption<RabbitsFoot>(),
            AncientOption<LuckyTreasureChest>(),
            AncientOption<RabbitsCage>()
        ));
}
