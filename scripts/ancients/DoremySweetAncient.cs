using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;
using TouhouAncients.Scripts.relics.DoremySweet;

namespace TouhouAncients.Scripts;

/// <summary>
/// 先古之民：哆来咪·苏伊特（Doremy Sweet）
/// 称号：梦之世界的支配者
///
/// 仅出现在第一幕（<see cref="ShowAct"/> == 1），与涅奥同池均匀抽取，
/// 相关注入逻辑见 <c>scripts/Patches/Act1AncientPoolPoolPatch.cs</c>。
///
/// 图标暂时沿用博丽灵梦的素材作为占位；补上
/// <c>images/icon/MapNode/DoremySweet_MapNode.png</c>、
/// <c>images/icon/MapNode/Outline/DoremySweet_MapNode.png</c>、
/// <c>images/icon/Character/DoremySweet.png</c>、
/// <c>images/icon/Character/Outline/DoremySweet.png</c> 后替换此处四条路径即可。
/// </summary>
public class DoremySweetAncient : TouhouAncientBase
{
    /// <summary>仅第一幕出现（具体幕数校验在基类 IsValidForAct 中完成）。</summary>
    public override int? ShowAct => 1;

    public override Color ButtonColor => new(0.45f, 0.35f, 0.7f, 0.75f);
    public override Color DialogueColor => new(0.55f, 0.45f, 0.85f, 1f);

    public override string? CustomMapIconPath => "res://images/icon/MapNode/HakureiReimu_MapNode.png";
    public override string? CustomMapIconOutlinePath => "res://images/icon/MapNode/Outline/HakureiReimu_MapNode.png";
    public override string? CustomRunHistoryIconPath => "res://images/icon/Character/HakureiReimu.png";
    public override string? CustomRunHistoryIconOutlinePath => "res://images/icon/Character/Outline/HakureiReimu.png";

    /// <summary>
    /// TODO（待定）：设计稿的「第一行 / 第二行 / 第三行 / 第四行」尚未映射到选项池。
    /// BaseLib 的 OptionPools 只支持 1 / 2 / 3 个池子，与此处的四行分组对不上，
    /// 因此在确认映射规则前先用「单池」占位：BaseLib 会从池中随机抽 3 个作为选项。
    /// 确认后请改为对应的 MakePool 组合。
    /// </summary>
    protected override OptionPools MakeOptionPools => new OptionPools(
        MakePool(
            AncientOption<IridescentDream>(),
            AncientOption<MeltingWaxDream>(),
            AncientOption<BlazingFlameDream>(),
            AncientOption<SinisterPactDream>(),
            AncientOption<FlowingSplendorDream>(),
            AncientOption<BloodbathDream>(),
            AncientOption<SupremeDream>(),
            AncientOption<RevivalDream>(),
            AncientOption<BygoneDream>()
        ));
}
