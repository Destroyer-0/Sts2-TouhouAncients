using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Events;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

[RegisterSharedAncient]
public class SaigyoujiYuyukoAncient : TouhouAncientBase
{
    public override int? ShowAct => 3;
    public override Color ButtonColor => new Color(0.3f, 0.3f, 0.6f, 0.6f);
    //public override Color ButtonColor => new Color(1f, 0.5f, 0.95f, 1f);
    public override Color DialogueColor => new Color(1f, 0.5f, 0.95f, 1f);


    /// <summary>
    /// 第一行（推进）：弘川之骨、天冠、反魂蝶
    /// 第二行（战斗）：弹幕的亡灵、幽灵折扇、墨染的樱花、幽魂酒�?    /// 第三行（后期）：人魂灯、黄泉期票、西行妖枯枝
    /// </summary>
    public override IEnumerable<EventOption> AllPossibleOptions =>
    [
        CreateModRelicOption<RepositoryOfHirokawa>(),
        CreateModRelicOption<SkyHat>(),
        CreateModRelicOption<SoulButterfly>(),
        CreateModRelicOption<DanmukuGhost>(),
        CreateModRelicOption<GhostFan>(),
        CreateModRelicOption<InkDyedCherryBlossoms>(),
        CreateModRelicOption<SoulSakeCup>(),
        CreateModRelicOption<SoulLattern>(),
        CreateModRelicOption<TicketToNetherworld>(),
        CreateModRelicOption<SaigyoujiBranch>()
    ];
}
