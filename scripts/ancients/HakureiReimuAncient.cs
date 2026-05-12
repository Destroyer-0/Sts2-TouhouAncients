using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

/// <summary>
/// 先古之民：博丽灵梦（Hakurei Reimu）
/// 红白巫女，博丽神社的现任巫女，解决异变的专家。
/// </summary>
public class HakureiReimuAncient : CustomAncientModel
{
    public override Color ButtonColor => new(0.9f, 0.25f, 0.25f, 0.7f);
    public override Color DialogueColor => new(0.9f, 0.25f, 0.25f);

    public override bool IsValidForAct(ActModel act)
    {
        if (TouhouAncientsConfig.BanReimu) return false;
        return act.ActNumber() == 2;
    }

    public override bool ShouldForceSpawn(ActModel act, AncientEventModel? rngChosenAncient)
    {
        return TouhouAncientsConfig.IsAncientForced<HakureiReimuAncient>(act.ActNumber());
    }

    public override string? CustomMapIconPath => "res://sprite/icon/WatariNina_MapNode.png";
    public override string? CustomMapIconOutlinePath => "res://sprite/icon/WatariNina_MapNode.png";
    public override string? CustomRunHistoryIconPath => "res://sprite/icon/HakureiReimu.png";
    public override string? CustomRunHistoryIconOutlinePath => "res://sprite/icon/HakureiReimu.png";

    protected override OptionPools MakeOptionPools => new OptionPools(
        MakePool(
            AncientOption<YinYangOrb>(),
            AncientOption<HakureiGohei>()
        ),
        MakePool(
            AncientOption<MiniShrine>(),
            AncientOption<DonateMoneyBox>()
        ),
        MakePool(
            AncientOption<SubspaceHole>(),
            AncientOption<SealingNeedle>()
        )
    );
}
