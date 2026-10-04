using System.Collections.Generic;
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
/// 相关注入逻辑见 <c>scripts/Patches/Act1AncientPoolPatch.cs</c>。
/// 她顶替的是涅奥，所以带 Modifier 的局（每日 / 自定义挑战）里这一页与原版涅奥一致：只有 Modifier 选项，
/// 不出现她自己的遗物选项与「离开梦境」（见 <see cref="TouhouAncientBase.GenerateInitialOptions"/>）。
/// </summary>
public class DoremySweetAncient : TouhouAncientBase
{
    /// <summary>仅第一幕出现（具体幕数校验在基类 IsValidForAct 中完成）。</summary>
    public override int? ShowAct => 1;

    public override Color ButtonColor => new(0.45f, 0.35f, 0.7f, 0.75f);
    public override Color DialogueColor => new(0.55f, 0.45f, 0.85f, 1f);

    public override string? CustomMapIconPath => "res://images/icon/MapNode/DoremySweet_MapNode.png";
    public override string? CustomMapIconOutlinePath => "res://images/icon/MapNode/Outline/DoremySweet_MapNode.png";
    public override string? CustomRunHistoryIconPath => "res://images/icon/Character/DoremySweet.png";
    public override string? CustomRunHistoryIconOutlinePath => "res://images/icon/Character/Outline/DoremySweet.png";

    protected override IReadOnlyList<TARelicOptionGroup> TARelicOptionPools =>
    [
        CreateTARelicOptionPool(
            TARelicOption<SupremeDream>(),
            TARelicOption<RevivalDream>(),
            TARelicOption<BygoneDream>(),
            TARelicOption<PioneerDream>(),
            TARelicOption<RevelationDream>()
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

    /// <summary>
    /// 追加在三个遗物选项之后的第 4 个选项「离开梦境」：放弃哆来咪的遗物，改为进入涅奥的房间。
    /// 逐玩家独立（只替换选择者自己在事件同步器里的那一份实例），其他玩家仍留在本页选自己的遗物，
    /// 互不影响；实现与存档语义见 <see cref="LeaveDreamReentry"/>。
    /// </summary>
    protected override IEnumerable<EventOption> ExtraOptions =>
    [
        LeaveDreamReentry.CreateOption(this)
    ];
}