using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

public class SaigyoujiYuyukoAncient : CustomAncientModel
{
    public override Color ButtonColor => new Color(1f, 0.5f, 0.95f, 0.5f);
    public override Color DialogueColor => new Color(1f, 0.5f, 0.95f, 1f);

    public override string? CustomMapIconPath => "res://images/icon/MapNode/WatariNina_MapNode.png";
    public override string? CustomMapIconOutlinePath => "res://images/icon/MapNode/WatariNina_MapNode.png";
    public override string? CustomRunHistoryIconPath => "res://images/icon/Character/SaigyoujiYuyuko.png";
    public override string? CustomRunHistoryIconOutlinePath => "res://images/icon/Character/Outline/SaigyoujiYuyuko.png";

    public override bool IsValidForAct(ActModel act)
    {
        return act.ActNumber() == 3;
    }

    public override bool ShouldForceSpawn(ActModel act, AncientEventModel? rngChosenAncient)
    {
        return TouhouAncientsConfig.IsAncientForced<SaigyoujiYuyukoAncient>(act.ActNumber());
    }

    /// <summary>
    /// 第一行（推进）：弘川之骨、天冠、反魂蝶
    /// 第二行（战斗）：弹幕的亡灵、幽灵折扇、墨染的樱花、幽魂酒盅
    /// 第三行（后期）：人魂灯、黄泉期票、西行妖枯枝
    /// </summary>
    protected override OptionPools MakeOptionPools => new OptionPools(
        MakePool(
            AncientOption<RepositoryOfHirokawa>(),
            AncientOption<SkyHat>(),
            AncientOption<SoulButterfly>()
        ),
        MakePool(
            AncientOption<DanmukuGhost>(),
            AncientOption<GhostFan>(),
            AncientOption<InkDyedCherryBlossoms>(),
            AncientOption<SoulSakeCup>()
        ),
        MakePool(
            AncientOption<SoulLattern>(),
            AncientOption<TicketToNetherworld>(),
            AncientOption<SaigyoujiBranch>()
        )
    );
}
