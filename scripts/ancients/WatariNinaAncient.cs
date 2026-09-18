using Godot;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

public class WatariNinaAncient : TouhouAncientBase
{
    public override int? ShowAct => 2;
    public override Color ButtonColor => new Color(0.05f, 0.07f, 0.2f, 0.5f);
    public override Color DialogueColor => new Color(0.05f, 0.07f, 0.2f, 1f);


    //public override string? CustomScenePath => "res://test/scenes/test_ancient.tscn";
    // 自定义地图图标和轮廓的路径
    public override string? CustomMapIconPath => "res://images/icon/MapNode/WatariNina_MapNode.png";

    public override string? CustomMapIconOutlinePath => "res://images/icon/MapNode/Outline/WatariNina_MapNode.png";

    // 历史记录图标路径
    public override string? CustomRunHistoryIconPath => "res://images/icon/Character/WatariNina.png";
    public override string? CustomRunHistoryIconOutlinePath => "res://images/icon/Character/Outline/WatariNina.png";
    /// <summary>三个选项共用同一个池（池内不重复）。</summary>
    protected override IReadOnlyList<TARelicOptionGroup> TARelicOptionPools =>
    [
        (MainPool, 3)
    ];

    private TARelicOptionPool MainPool => CreateTARelicOptionPool(
        TARelicOption<Zhangeweilaiba>(),
        TARelicOption<Yiyandingzhen>(),
        TARelicOption<Huoyantuxi>(),
        TARelicOption<Bileihaopaiduozhua>(),
        TARelicOption<Baibaixiangxiangruanruan>(),
        TARelicOption<Geishehuaxiaojie>(),
        TARelicOption<Sheyaotebieqiang>(),
        TARelicOption<Yishixingqile>(),
        TARelicOption<Yonghengkaijiawangchaole>(),
        TARelicOption<Dongnichangshu>()
        );
}