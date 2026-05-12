using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

public class KotiyaSanaeAncient : CustomAncientModel
{
    public override Color ButtonColor => new(0.2275f, 0.6157f, 0.2078f, 0.7f);
    public override Color DialogueColor => new(0.2275f, 0.6157f, 0.2078f);

    public override string? CustomMapIconPath => "res://sprite/icon/WatariNina_MapNode.png";

    public override string? CustomMapIconOutlinePath => "res://sprite/icon/WatariNina_MapNode.png";

    // 历史记录图标路径
    public override string? CustomRunHistoryIconPath => "res://sprite/icon/KotiyaSanae.png";
    public override string? CustomRunHistoryIconOutlinePath => "res://sprite/icon/KotiyaSanae.png";

    public override bool IsValidForAct(ActModel act)
    {
        if (TouhouAncientsConfig.BanSanae) return false;
        return base.IsValidForAct(act);
    }

    public override bool ShouldForceSpawn(ActModel act, AncientEventModel? rngChosenAncient)
    {
        return TouhouAncientsConfig.IsAncientForced<KotiyaSanaeAncient>(act.ActNumber());
    }

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