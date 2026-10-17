using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 虚构都市的抽到计数：抽到一张就叠一层，满 <see cref="Threshold"/> 层永久失去力量与敏捷后清零重算。
/// 取 PowerType.None：Debuff 会被人工制品挡下、Buff 会被增益类效果放大，都会让计数失真（同 LuckyTrickCountPower）。
/// </summary>
public sealed class FictionalCityCountPower : TouhouAncientPowerModel
{
    private const int Threshold = 3;

    private const string RemainingKey = "Remaining";

    public override PowerType Type => PowerType.None;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar(RemainingKey, Threshold)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<StrengthPower>(), HoverTipFactory.FromPower<DexterityPower>()];

    /// <summary>
    /// 首次施加与每次叠加都会走这里。达阈值则兑现扣减并移除自身（下次抽到再从 1 层开始）。
    /// 扣减用 StrengthPower/DexterityPower 负值，即本场永久，不会随回合结束恢复。
    /// </summary>
    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power,
        decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power != this) return;

        if (amount > 0m && base.Amount >= Threshold)
        {
            Flash();
            await PowerCmd.Apply<StrengthPower>(choiceContext, base.Owner, -1m, base.Owner, null);
            await PowerCmd.Apply<DexterityPower>(choiceContext, base.Owner, -1m, base.Owner, null);
            await PowerCmd.Remove(this);
            return;
        }

        base.DynamicVars[RemainingKey].BaseValue = System.Math.Max(0, Threshold - base.Amount);
        InvokeDisplayAmountChanged();
    }
}
