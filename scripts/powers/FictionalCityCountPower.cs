using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 虚构都市的抽到计数：抽到一张就叠一层，满 <see cref="Threshold"/> 层永久失去力量与敏捷并立刻结束回合，然后清零重算。
/// 取 PowerType.None：Debuff 会被人工制品挡下、Buff 会被增益类效果放大，都会让计数失真（同 LuckyTrickCountPower）。
/// </summary>
public sealed class FictionalCityCountPower : TouhouAncientPowerModel
{
    private const int Threshold = 5;

    private const string RemainingKey = "Remaining";

    public override PowerType Type => PowerType.None;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar(RemainingKey, Threshold)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<StrengthPower>(), HoverTipFactory.FromPower<DexterityPower>()];

    /// <summary>
    /// 首次施加与每次叠加都会走这里。达阈值则兑现扣减、移除自身并结束回合（下次抽到再从 1 层开始）。
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

            // 抽牌多半发生在回合准备阶段，CombatManager 会把这个 EndTurn 延迟到准备结束再执行
            Player? player = base.Owner.Player;
            await PowerCmd.Remove(this);
            if (player != null && !CombatManager.Instance.IsOverOrEnding)
            {
                PlayerCmd.EndTurn(player, canBackOut: false);
            }
            return;
        }

        base.DynamicVars[RemainingKey].BaseValue = System.Math.Max(0, Threshold - base.Amount);
        InvokeDisplayAmountChanged();
    }
}
