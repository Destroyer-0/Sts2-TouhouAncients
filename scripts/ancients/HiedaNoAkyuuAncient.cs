using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Events;
using TouhouAncients.Scripts.relics.Akyuu;

namespace TouhouAncients.Scripts;

/// <summary>
/// 先古之民：稗田阿求（Hieda no Akyuu）
/// 称号：时间线的撰写者
///
/// 仅出现在第一幕（<see cref="ShowAct"/> == 1），与涅奥、哆来咪同池均匀抽取，
/// 相关注入逻辑见 <c>scripts/Patches/Act1AncientPoolPatch.cs</c>。
/// 第 4 个选项「前去面见涅奥」与哆来咪的「离开梦境」共用一套实现
/// （<see cref="LeaveDreamReentry"/>，按 <c>{Id.Entry}.pages.INITIAL.options.{选项名}</c> 取文案）。
/// </summary>
public class HiedaNoAkyuuAncient : TouhouAncientBase
{
    /// <summary>仅第一幕出现（具体幕数校验在基类 IsValidForAct 中完成）。</summary>
    public override int? ShowAct => 1;

    public override Color ButtonColor => new(0.2f, 0.2f, 0.2f, 0.5f);
    public override Color DialogueColor => new(0.9f, 0.35f, 1f, 1f);

    public override string? CustomMapIconPath => "res://images/icon/MapNode/FutatsuiwaMamizou_MapNode.png";
    public override string? CustomMapIconOutlinePath => "res://images/icon/MapNode/Outline/FutatsuiwaMamizou_MapNode.png";
    public override string? CustomRunHistoryIconPath => "res://images/icon/Character/HiedaNoAkyuu.png";
    public override string? CustomRunHistoryIconOutlinePath => "res://images/icon/Character/Outline/HiedaNoAkyuu.png";

    protected override IReadOnlyList<TARelicOptionGroup> TARelicOptionPools =>
    [
        (MainPool, 3)
    ];

    private TARelicOptionPool MainPool => CreateTARelicOptionPool(
            TARelicOption<MiareBrush>(),
            TARelicOption<MiareInk>(),
            TARelicOption<MiarePaper>(),
            TARelicOption<MiareInkstone>(),
            TARelicOption<YougakudanCollection>(),
            TARelicOption<ReadingGlasses>(),
            TARelicOption<YokaiBestiary>(),
            TARelicOption<CalmingTea>(),
            TARelicOption<DetectiveManuscript>(),
            TARelicOption<SignedShikishi>(),
            TARelicOption<CamelliaHairpin>(),
            TARelicOption<HardcoverBinding>(),
            TARelicOption<SakuraComb>(),
            TARelicOption<AncientSpecimen>(),
            TARelicOption<BlankRoster>(),
            TARelicOption<BellBookmark>()
            )
    ;

    /// <summary>
    /// 追加在三个遗物选项之后的第 4 个选项「前去面见涅奥」：放弃阿求的遗物，改为进入涅奥的房间。
    /// 逐玩家独立，语义与实现见 <see cref="LeaveDreamReentry"/>。
    /// </summary>
    protected override IEnumerable<EventOption> ExtraOptions =>
    [
        LeaveDreamReentry.CreateOption(this, LeaveDreamReentry.MeetNeowOptionName)
    ];
}
