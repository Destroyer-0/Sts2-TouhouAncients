using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

public class KotiyaSanaeAncient : TouhouAncientBase
{
    public override int? ShowAct => null;
    public override Color ButtonColor => new(0.2275f, 0.6157f, 0.2078f, 0.6f);
    public override Color DialogueColor => new(0.2275f, 0.6157f, 0.2078f, 1f);

    public override string? CustomMapIconPath => "res://images/icon/MapNode/WatariNina_MapNode.png";

    public override string? CustomMapIconOutlinePath => "res://images/icon/MapNode/WatariNina_MapNode.png";

    // 历史记录图标路径
    public override string? CustomRunHistoryIconPath => "res://images/icon/Character/KotiyaSanae.png";
    public override string? CustomRunHistoryIconOutlinePath => "res://images/icon/Character/Outline/KotiyaSanae.png";

    // protected override OptionPools MakeOptionPools => new OptionPools(
    //     MakePool(
    //         AncientOption<Zhangeweilaiba>(),
    //         AncientOption<Yiyandingzhen>(),
    //         AncientOption<Huoyantuxi>(),
    //         AncientOption<Bileihaopaiduozhua>(),
    //         AncientOption<Baibaixiangxiangruanruan>(),
    //         AncientOption<Geishehuaxiaojie>(),
    //         AncientOption<Sheyaotebieqiang>(),
    //         AncientOption<Yishixingqile>(),
    //         AncientOption<Yonghengkaijiawangchaole>(),
    //         AncientOption<Zhihuijizhongbing>()
    //     ));
    //
    protected override OptionPools MakeOptionPools => new OptionPools(
        MakePool(
            AncientOption<MoriyaGohei>(),
            AncientOption<DayKakusei>(),
            AncientOption<WindPriestessWine>()
        ),
        MakePool(
            AncientOption<SnakeAmulet>(),
            AncientOption<FrogAmulet>(),
            AncientOption<HisoutensokuModel>()
        ),
        MakePool(
            AncientOption<SailorSuit>(),
            AncientOption<GiftFromMountain>(),
            AncientOption<MiracleNoble>()
        )
    );
}