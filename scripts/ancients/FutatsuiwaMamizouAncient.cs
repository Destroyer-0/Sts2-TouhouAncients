using Godot;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

/// <summary>
/// 先古之民：二岩猯藏（Futatsuiwa Mamizou）
/// 佐渡的二枚岩大狸子，擅长变化的妖怪狸猫。
/// 出现在二层或三层。
/// </summary>
public class FutatsuiwaMamizouAncient : TouhouAncientBase
{
    public override int? ShowAct => null;
    public override Color ButtonColor => new(0.545f, 0.353f, 0.169f, 0.7f);
    public override Color DialogueColor => new(0.545f, 0.353f, 0.169f, 1f);

    public override string? CustomMapIconPath => "res://images/icon/MapNode/HakureiReimu_MapNode.png";
    public override string? CustomMapIconOutlinePath => "res://images/icon/MapNode/Outline/HakureiReimu_MapNode.png";

    public override string? CustomRunHistoryIconPath => "res://images/icon/Character/FutatsuiwaMamizou.png";
    public override string? CustomRunHistoryIconOutlinePath => "res://images/icon/Character/Outline/FutatsuiwaMamizou.png";

    /// <summary>三个选项共用同一个池（池内不重复）。</summary>
    protected override IReadOnlyList<TARelicOptionGroup> TARelicOptionPools =>
    [
        (MainPool, 3)
    ];

    private TARelicOptionPool MainPool => CreateTARelicOptionPool(
        TARelicOption<ReliableTanukiDisciple>(),
        TARelicOption<CrimsonCloudKnuckles>(),
        TARelicOption<SunkenAnchorGhost>(),
        TARelicOption<NidomiteiSpecialBlend>(),
        TARelicOption<PsychicTelekinesis>(),
        TARelicOption<WickedHermitHairpin>(),
        TARelicOption<DowsingRod>(),
        TARelicOption<DragonVeinVessel>(),
        TARelicOption<DisillusionTrident>(),
        TARelicOption<TunakiSmokingPipe>(),
        TARelicOption<HeavyDice>()
        //TARelicOption<OneEyedKarakasa>(),
        //TARelicOption<HyakkiYagyoScroll>(),
        //TARelicOption<NohMask>()
        );
}

