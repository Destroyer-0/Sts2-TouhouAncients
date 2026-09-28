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
        TARelicOption<Dongnichangshu>(),
        TARelicOption<Wuqiongwujindefennu>().Prep((Wuqiongwujindefennu relic) =>
        {
            // 事件描述要按当前人数写出群情激愤 / 愤怒，必须在选项渲染前填好；
            // 此阶段遗物自身的 Owner 尚未设置，用古代事件自己的 Owner。
            if (Owner != null) relic.SetupForPlayer(Owner);
        }),
        TARelicOption<Jinsiliangjideyingzhang>()
        );
}