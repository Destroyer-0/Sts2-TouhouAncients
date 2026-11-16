using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using TouhouAncients.Scripts.monsters;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 无心璨炎：纯狐的破防标记。
/// 首次造成未被格挡的伤害、或生命值被压到阈值以下时移除自身；只要被移除（含其它手段），
/// 清除全部力量并进入二阶段——由 <see cref="AfterRemoved"/> 统一处理。
/// Amount 为生命值触发阈值，与怪物生命同样按玩家数与幕系数放大（参考原版犁地能力）。
/// </summary>
public sealed class HeartlessBlazePower : TouhouAncientPowerModel
{
    /// <summary>移除原因：1=未格挡伤害，2=生命阈值，0=其它手段移除（默认）。</summary>
    private int _banter;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldScaleInMultiplayer => true;

    /// <summary>图标暂用怨仇的图，后续替换。</summary>
    public override string? CustomPackedIconPath => TouhouAncientCmd.CheckPathExists("res://images/icon/power/YuanChouPower.png");
    public override string? CustomBigIconPath => TouhouAncientCmd.CheckPathExistsWithFallback2("res://images/icon/power/BigIcon/YuanChouPower.png", CustomPackedIconPath);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<StrengthPower>()];

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer,
        DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (dealer != base.Owner || result.UnblockedDamage <= 0) return;
        await Trigger(1);
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != base.Owner || result.UnblockedDamage <= 0) return;
        if (target.CurrentHp > base.Amount) return;
        await Trigger(2);
    }

    /// <summary>只移除自身，二阶段统一由 AfterRemoved 处理。</summary>
    private async Task Trigger(int banter)
    {
        _banter = banter;
        Flash();
        await PowerCmd.Remove(this);
    }

    /// <summary>死亡/清场走的是跳过 AfterRemoved 的路径，所以这里只在真正被移除时进二阶段。</summary>
    public override async Task AfterRemoved(Creature oldOwner)
    {
        if (oldOwner.IsDead || oldOwner.Monster is not JunkoMonster junko) return;
        await PowerCmd.Remove<StrengthPower>(oldOwner);
        junko.EnterPhaseTwo(_banter);
    }
}
