using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

public class WatariNinaAncient : CustomAncientModel
{
    public override Color ButtonColor => new Color(0.05f, 0.07f, 0.2f, 0.8f);

    public override Color DialogueColor => new Color("384d7a");

    //public override string? CustomScenePath => "res://test/scenes/test_ancient.tscn";
    // 自定义地图图标和轮廓的路径
    public override string? CustomMapIconPath => "res://icon/WatariNina_MapNode.png";

    public override string? CustomMapIconOutlinePath => "res://icon/WatariNina_MapNode.png";

    // 历史记录图标路径
    public override string? CustomRunHistoryIconPath => "res://icon/WatariNina.png";
    public override string? CustomRunHistoryIconOutlinePath => "res://icon/WatariNina.png";
    public override bool IsValidForAct(ActModel act) => act.ActNumber() == 2;

    public override bool ShouldForceSpawn(ActModel act, AncientEventModel? rngChosenAncient) => act.ActNumber() == 2;

    protected override OptionPools MakeOptionPools => new OptionPools(OptionPool1);
    
    private WeightedList<AncientOption> OptionPool1 =>
    [
        AncientOption<Zhangeweilaiba>(),
        AncientOption<Yiyandingzhen>(),
        AncientOption<Huoyantuxi>(),
        AncientOption<Bileihaopaiduozhua>(),
        AncientOption<Baibaixiangxiangruanruan>(),
        AncientOption<Geishehuaxiaojie>(),
        AncientOption<Sheyaotebieqiang>(),
        AncientOption<Yishixingqile>(),
        AncientOption<Yonghengkaijiawangchaole>(),
        AncientOption<Zhihuijizhongbing>(),
    ];
    //
    // private WeightedList<AncientOption> OptionPool2 =>
    // [
    //     AncientOption<Zhangeweilaiba>(3),
    //     AncientOption<Zhangeweilaiba>(3),
    //     AncientOption<Zhangeweilaiba>(2)
    // ];
    //
    // private WeightedList<AncientOption> OptionPool3
    // {
    //     get
    //     {
    //         WeightedList<AncientOption> list = new WeightedList<AncientOption>();
    //
    //         list.Add(AncientOption<Zhangeweilaiba>(), 3);
    //         list.Add(AncientOption<Zhangeweilaiba>(), 1);
    //         list.Add(AncientOption<Zhangeweilaiba>(), 2);
    //         
    //         return list;
    //     }
    // }


    // public override IEnumerable<EventOption> AllPossibleOptions =>
    // [
    //     RelicOption<Zhangeweilaiba>(),
    //     RelicOption<Zhangeweilaiba>(),
    //     RelicOption<Zhangeweilaiba>(),
    //     RelicOption<Zhangeweilaiba>(),
    //     RelicOption<Zhangeweilaiba>(),
    //     RelicOption<Zhangeweilaiba>(),
    //     RelicOption<Zhangeweilaiba>(),
    //     RelicOption<Zhangeweilaiba>(),
    //     RelicOption<Zhangeweilaiba>(),
    //     RelicOption<Zhangeweilaiba>()
    // ];
}