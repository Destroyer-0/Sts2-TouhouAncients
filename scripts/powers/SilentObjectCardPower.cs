using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 不语之物的本体：持有该不语之物所代表的卡牌，悬停随从时预览该牌
/// （机制参考原版偷窃草蜢的 SwipePower：Creature.HoverTips 会聚合各 Power 的 ExtraHoverTips）。
/// </summary>
public class SilentObjectCardPower : TouhouAncientPowerModel
{
    /// <summary>图标复用原版 SwipePower（卡牌被敌方持有）。</summary>
    public override string? CustomPackedIconPath => TouhouAncientCmd.CheckPathExists("res://images/atlases/power_atlas.sprites/swipe_power.tres");

    public override string? CustomBigIconPath => TouhouAncientCmd.CheckPathExists("res://images/powers/swipe_power.png");

    private CardModel? _embodiedCard;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override bool ShouldPlayVfx => false;

    /// <summary>所代表的卡牌。</summary>
    public CardModel? EmbodiedCard
    {
        get => _embodiedCard;
        set
        {
            AssertMutable();
            _embodiedCard = value;
        }
    }

    /// <summary>悬停预览：被代表的牌。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        EmbodiedCard == null ? [] : [HoverTipFactory.FromCard(EmbodiedCard)];
}
