using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

public class HinanawiTenshiAncient : CustomAncientModel
{
    public override Color ButtonColor => new(0.0f, 0.63f, 1f, 0.8f);
    public override Color DialogueColor => new(0.0f, 0.63f, 1f);

    public override string? CustomMapIconPath => "res://sprite/icon/WatariNina_MapNode.png";
    public override string? CustomMapIconOutlinePath => "res://sprite/icon/WatariNina_MapNode.png";
    public override string? CustomRunHistoryIconPath => "res://sprite/icon/KotiyaSanae.png";
    public override string? CustomRunHistoryIconOutlinePath => "res://sprite/icon/KotiyaSanae.png";

    public override bool IsValidForAct(ActModel act)
    {
        if (TouhouAncientsConfig.BanTenshi) return false;
        return act.ActNumber() == 3;
    }

    public override bool ShouldForceSpawn(ActModel act, AncientEventModel? rngChosenAncient)
    {
        return TouhouAncientsConfig.IsAncientForced<HinanawiTenshiAncient>(act.ActNumber());
    }

    // Pool 1: 普通池
    // Pool 2: 稀有池
    // Pool 3: 先古之民池
    protected override OptionPools MakeOptionPools => new OptionPools(
        MakePool(
            AncientOption<MysticFortunePeach>(),
            AncientOption<CurseBreakerQi>()
        ),
        MakePool(
            AncientOption<FirmamentSash>(),
            AncientOption<SupremeHeavenSeal>()
        ),
        MakePool(
            AncientOption<HisouSword>()
            //AncientOption<KeystoneFloatingCannon>()
        )
    );
}
