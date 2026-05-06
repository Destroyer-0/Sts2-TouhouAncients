using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

public class KotiyaSanaeAncient : CustomAncientModel
{
    public override Color ButtonColor => new(0.58f, 1f, 0.66f, 0.5f);
    public override Color DialogueColor => new(0.58f, 1f, 0.66f);

    public override string? CustomMapIconPath => "res://icon/WatariNina_MapNode.png";

    public override string? CustomMapIconOutlinePath => "res://icon/WatariNina_MapNode.png";

    // 历史记录图标路径
    public override string? CustomRunHistoryIconPath => "res://icon/WatariNina.png";
    public override string? CustomRunHistoryIconOutlinePath => "res://icon/WatariNina.png";
    
    protected override OptionPools MakeOptionPools { get; } = new OptionPools(
        MakePool(
            AncientOption<MoriyaGohei>(),
            AncientOption<DayKakusei>()
        ),
        MakePool(
            AncientOption<SnakeAmulet>(),
            AncientOption<FrogAmulet>()
        ),
        MakePool(
            AncientOption<SailorSuit>(),
            AncientOption<GiftFromMountain>()
        )
    );
}