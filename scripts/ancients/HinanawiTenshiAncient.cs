using Godot;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

public class HinanawiTenshiAncient : TouhouAncientBase
{
    public override int? ShowAct => 3;
    public override Color ButtonColor => new(0.0f, 0.4f, 0.8f, 0.6f);
    public override Color DialogueColor => new(0.0f, 0.63f, 1f, 1f);

    public override string? CustomMapIconPath => "res://images/icon/MapNode/HinanawiTenshi_MapNode.png";
    public override string? CustomMapIconOutlinePath => "res://images/icon/MapNode/Outline/HinanawiTenshi_MapNode.png";
    public override string? CustomRunHistoryIconPath => "res://images/icon/Character/HinanawiTenshi.png";
    public override string? CustomRunHistoryIconOutlinePath => "res://images/icon/Character/Outline/HinanawiTenshi.png";

    /// <summary>
    /// 池子2依照玩家是否是储君选择给予 天界冷漠（储君权重3，其他人权重1）/天宇诏令（储君权重0，其他人权重2）
    /// </summary>
    protected override IReadOnlyList<TARelicOptionGroup> TARelicOptionPools =>
    [
        CreateTARelicOptionPool(
            TARelicOption<MysticFortunePeach>(),
            TARelicOption<HolyArmor>(),
            TARelicOption<HeavenlyRevelation>()
            //, TARelicOption<CurseBreakerQi>()
            ),
        CreateTARelicOptionPool(
            // 权重：FirmamentSash / CurseBreakerQi 为 3；CelestialIndifference 在天子时为 3、否则 2；
            // CosmicDecree 在天子时不出现（权重 0）。
            TARelicOption<FirmamentSash>(3),
            TARelicOption<CurseBreakerQi>(3),
            TARelicOption<CelestialIndifference>(Owner?.Character is Regent ? 3 : 1),
            TARelicOption<CosmicDecree>(Owner?.Character is Regent ? 0 : 2)
            //, TARelicOption<SupremeHeavenSeal>()
            ),
        CreateTARelicOptionPool(
            TARelicOption<HisouSword>(),
            TARelicOption<KeystoneFloatingCannon>(),
            TARelicOption<HeavenlyCloudRobe>()
            //TARelicOption<KeystoneFloatingCannon>()
            )
    ];
}
