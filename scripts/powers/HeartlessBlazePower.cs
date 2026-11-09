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
/// 首次造成未被格挡的伤害、或生命值被压到阈值以下时，清除全部力量并移除此能力（进入二阶段）。
/// Amount 为生命值触发阈值，与怪物生命同样按玩家数与幕系数放大（参考原版犁地能力）。
/// </summary>
public sealed class HeartlessBlazePower : TouhouAncientPowerModel
{
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
        await Trigger(fromUnblockedDamage: true);
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != base.Owner || result.UnblockedDamage <= 0) return;
        if (target.CurrentHp > base.Amount) return;
        await Trigger(fromUnblockedDamage: false);
    }

    /// <summary>触发后自身即被移除，两个入口天然不会重复执行。</summary>
    private async Task Trigger(bool fromUnblockedDamage)
    {
        Flash();
        await PowerCmd.Remove<StrengthPower>(base.Owner);
        if (base.Owner.Monster is JunkoMonster junko)
        {
            junko.EnterPhaseTwo(fromUnblockedDamage);
        }
        await PowerCmd.Remove(this);
    }
}
