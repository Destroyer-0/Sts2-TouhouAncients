using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 我会还给你的！：魔理沙的偷牌暂存能力。
/// 每个实例暂存一名玩家被偷走的 1 张牌，极限火花·蓄力时归还到手牌并移除该能力。
/// </summary>
public class IWillReturnItPower : TouhouAncientPowerModel
{
    /// <summary>
    /// 图标直接复用原版 SwipePower 的图标（小图标为 power_atlas 图集子资源，大图标为 images/powers 下的 png）。
    /// </summary>
    public override string? CustomPackedIconPath => TouhouAncientCmd.CheckPathExists("res://images/atlases/power_atlas.sprites/swipe_power.tres");
    public override string? CustomBigIconPath => TouhouAncientCmd.CheckPathExists("res://images/powers/swipe_power.png");

    private CardModel? _stolenCard;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    /// <summary>被偷走的牌（每个实例 1 张）。</summary>
    public CardModel? StolenCard
    {
        get => _stolenCard;
        set
        {
            AssertMutable();
            _stolenCard = value;
        }
    }

    /// <summary>
    /// 悬停提示：显示被偷走的牌（参考原版 SwipePower）。
    /// </summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        StolenCard == null ? [] : [HoverTipFactory.FromCard(StolenCard)];
}
