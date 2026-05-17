using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

public class HinanawiTenshiAncient : CustomAncientModel
{
    public override Color ButtonColor => new(0.0f, 0.63f, 1f, 0.8f);
    public override Color DialogueColor => new(0.0f, 0.63f, 1f);

    public override string? CustomMapIconPath => "res://sprite/icon/MapNode/WatariNina_MapNode.png";
    public override string? CustomMapIconOutlinePath => "res://sprite/icon/MapNode/WatariNina_MapNode.png";
    public override string? CustomRunHistoryIconPath => "res://sprite/icon/Character/HinanawiTenshi.png";
    public override string? CustomRunHistoryIconOutlinePath => "res://sprite/icon/Character/Outline/HinanawiTenshi.png";

    public override bool IsValidForAct(ActModel act)
    {
        if (TouhouAncientsConfig.BanTenshi) return false;
        return act.ActNumber() == 3;
    }

    public override bool ShouldForceSpawn(ActModel act, AncientEventModel? rngChosenAncient)
    {
        return TouhouAncientsConfig.IsAncientForced<HinanawiTenshiAncient>(act.ActNumber());
    }

    /// <summary>
    /// 池子2依照玩家是否是储君选择给予 天界冷漠（储君权重3，其他人权重1）/天宇诏令（储君权重0，其他人权重2）
    /// </summary>
    protected override OptionPools MakeOptionPools => new OptionPools(
        MakePool(
            AncientOption<MysticFortunePeach>(),
            AncientOption<HolyArmor>()
            //, AncientOption<CurseBreakerQi>()
        ),
        MakePool(
            AncientOption<FirmamentSash>(weight:3),
            AncientOption<CurseBreakerQi>(weight:3),
            AncientOption<CelestialIndifference>(weight: base.Owner == null ? 2 : base.Owner.Character is Regent ? 3 : 1),
            AncientOption<CosmicDecree>(weight: base.Owner == null ? 2 : base.Owner.Character is Regent ? 0 : 2)
            //, AncientOption<SupremeHeavenSeal>()
        ),
        MakePool(
            AncientOption<HisouSword>(),
            AncientOption<KeystoneFloatingCannon>()
            //AncientOption<KeystoneFloatingCannon>()
        )
    );
}
