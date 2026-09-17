using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;
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
    /// BaseLib 的 OptionPools 只支持 1 / 2 / 3 个池子，与此处的四行分组对不上，
    /// 因此在确认映射规则前先用「单池」占位：BaseLib 会从池中随机抽 3 个作为选项。
    /// 确认后请改为对应的 MakePool 组合。
    /// </summary>
    protected override OptionPools MakeOptionPools => new OptionPools(
        MakePool(
            AncientOption<SupremeDream>(),
            AncientOption<RevivalDream>(),
            AncientOption<BygoneDream>()
        ),
        MakePool(
            AncientOption<IridescentDream>(relicPrep: PrepIridescentDream),
            AncientOption<MeltingWaxDream>(),
            AncientOption<BlazingFlameDream>()),
        MakePool(
            AncientOption<SinisterPactDream>(),
            AncientOption<FlowingSplendorDream>(),
            AncientOption<BloodbathDream>())
        );

    /// <summary>
    /// 虹光之梦的文本依赖当前角色的初始遗物与初始卡牌：描述里的
    /// <c>{StarterRelic.StringValue:cond:具体名称|泛化文本}</c> 只有在事件界面渲染之前
    /// 就填好 <c>StringValue</c>，玩家在选项提示里看到的才是具体名称。
    ///
    /// BaseLib 的 <c>AncientOption&lt;T&gt;</c> 把这个时机通过 <c>relicPrep</c> 暴露出来，
    /// 对应原版 Orobas 事件里调用 <c>TouchOfOrobas.SetupForPlayer</c> 的做法。
    /// 该阶段遗物自身的 Owner 尚未设置，所以这里用 ancient 事件自己的 <see cref="Owner"/>；
    /// 若 Owner 为空（例如在没有对局上下文时枚举选项），则跳过预准备，文本退回泛化形式。
    /// </summary>
    private RelicModel PrepIridescentDream(IridescentDream relic)
    {
        if (Owner != null)
        {
            relic.SetupForPlayer(Owner);
        }

        return relic;
    }
}