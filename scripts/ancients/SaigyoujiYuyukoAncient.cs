using Godot;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

public class SaigyoujiYuyukoAncient : TouhouAncientBase
{
    public override int? ShowAct => 3;
    public override Color ButtonColor => new Color(0.3f, 0.3f, 0.6f, 0.6f);
    //public override Color ButtonColor => new Color(1f, 0.5f, 0.95f, 1f);
    public override Color DialogueColor => new Color(1f, 0.5f, 0.95f, 1f);

    public override string? CustomMapIconPath => "res://images/icon/MapNode/SaigyoujiYuyuko_MapNode.png";
    public override string? CustomMapIconOutlinePath => "res://images/icon/MapNode/Outline/SaigyoujiYuyuko_MapNode.png";
    public override string? CustomRunHistoryIconPath => "res://images/icon/Character/SaigyoujiYuyuko.png";
    public override string? CustomRunHistoryIconOutlinePath => "res://images/icon/Character/Outline/SaigyoujiYuyuko.png";

    /// <summary>
    /// 第一行（推进）：弘川之骨、天冠、反魂蝶
    /// 第二行（战斗）：弹幕的亡灵、幽灵折扇、墨染的樱花、幽魂酒盅
    /// 第三行（后期）：人魂灯、黄泉期票、西行妖枯枝
    /// </summary>
    protected override IReadOnlyList<TARelicOptionGroup> TARelicOptionPools =>
    [
        CreateTARelicOptionPool(
            TARelicOption<RepositoryOfHirokawa>(),
            TARelicOption<SkyHat>(),
            TARelicOption<SoulButterfly>()
            ),
        CreateTARelicOptionPool(
            TARelicOption<DanmukuGhost>(),
            TARelicOption<GhostFan>(),
            TARelicOption<InkDyedCherryBlossoms>(),
            TARelicOption<SoulSakeCup>()
            ),
        CreateTARelicOptionPool(
            TARelicOption<SoulLattern>(),
            TARelicOption<TicketToNetherworld>(),
            TARelicOption<SaigyoujiBranch>()
            )
    ];
}
