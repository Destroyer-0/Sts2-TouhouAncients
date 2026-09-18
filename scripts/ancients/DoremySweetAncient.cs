using Godot;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.relics.DoremySweet;

namespace TouhouAncients.Scripts;

/// <summary>
/// 先古之民：哆来咪·苏伊特（Doremy Sweet）
/// 称号：梦之世界的支配者
///
/// 仅出现在第一幕（<see cref="ShowAct"/> == 1），与涅奥同池均匀抽取，
/// 相关注入逻辑见 <c>scripts/Patches/Act1AncientPoolPoolPatch.cs</c>。
/// </summary>
public class DoremySweetAncient : TouhouAncientBase
{
    /// <summary>仅第一幕出现（具体幕数校验在基类 IsValidForAct 中完成）。</summary>
    public override int? ShowAct => 1;

    public override Color ButtonColor => new(0.45f, 0.35f, 0.7f, 0.75f);
    public override Color DialogueColor => new(0.55f, 0.45f, 0.85f, 1f);

    public override string? CustomMapIconPath => "res://images/icon/MapNode/HakureiReimu_MapNode.png";
    public override string? CustomMapIconOutlinePath => "res://images/icon/MapNode/Outline/HakureiReimu_MapNode.png";
    public override string? CustomRunHistoryIconPath => "res://images/icon/Character/DoremySweet.png";
    public override string? CustomRunHistoryIconOutlinePath => "res://images/icon/Character/Outline/DoremySweet.png";

    /// <summary>
    /// 本 Ancient 的选项（三行 = 三个选项）。
    /// 虹光之梦的描述含 <c>{StarterRelic.StringValue:cond:具体名称|泛化文本}</c>，必须在事件界面渲染之前
    /// 就填好 StringValue，玩家在选项里看到的才是具体名称（对应原版 Orobas 的 <c>TouchOfOrobas.SetupForPlayer</c>），
    /// 因此用 <c>Prep</c> 预准备；此阶段遗物自身的 Owner 尚未设置，用古代事件自己的 <see cref="Owner"/>。
    /// 「先驱之梦」的可用性由遗物自己的 <see cref="TouhouAncientRelics.CanAppear"/> 声明，条件不满足时基类会把它从候选中剔除。
    /// </summary>
    protected override IReadOnlyList<TARelicOptionGroup> TARelicOptionPools =>
    [
        CreateTARelicOptionPool(
            TARelicOption<SupremeDream>(),
            TARelicOption<RevivalDream>(),
            TARelicOption<BygoneDream>(),
            TARelicOption<PioneerDream>()
            ),
        CreateTARelicOptionPool(
            TARelicOption<IridescentDream>().Prep((IridescentDream relic) =>
            {
                if (Owner != null) relic.SetupForPlayer(Owner);
            }),
            TARelicOption<MeltingWaxDream>(),
            TARelicOption<BlazingFlameDream>()
            ),
        CreateTARelicOptionPool(
            TARelicOption<SinisterPactDream>(),
            TARelicOption<FlowingSplendorDream>(),
            TARelicOption<BloodbathDream>()
            )
    ];
}