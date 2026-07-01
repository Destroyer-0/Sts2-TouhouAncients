using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Events;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using MegaCrit.Sts2.Core.Models.Relics;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

[RegisterSharedAncient]
public class WatariNinaAncient : TouhouAncientBase
{
    public override int? ShowAct => 2;
    public override Color ButtonColor => new Color(0.05f, 0.07f, 0.2f, 0.5f);
    public override Color DialogueColor => new Color(0.05f, 0.07f, 0.2f, 1f);


    public override IEnumerable<EventOption> AllPossibleOptions =>
    [
        CreateModRelicOption<Zhangeweilaiba>(),
        CreateModRelicOption<Yiyandingzhen>(),
        CreateModRelicOption<Huoyantuxi>(),
        CreateModRelicOption<Bileihaopaiduozhua>(),
        CreateModRelicOption<Baibaixiangxiangruanruan>(),
        CreateModRelicOption<Geishehuaxiaojie>(),
        CreateModRelicOption<Sheyaotebieqiang>(),
        CreateModRelicOption<Yishixingqile>(),
        CreateModRelicOption<Yonghengkaijiawangchaole>(),
        CreateModRelicOption<Zhihuijizhongbing>()
    ];

    // TODO: Restore weighted pool logic using RitsuLib API if needed
}