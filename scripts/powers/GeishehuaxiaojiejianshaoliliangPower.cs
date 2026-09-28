using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;

namespace TouhouAncients.Scripts.powers;

public class GeishehuaxiaojiejianshaoliliangPower : TouhouAncientPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerInstanceType InstanceType => PowerInstanceType.InstancedPerApplier;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new StringVar("Applier")];
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<StrengthPower>()];
    
    /// <summary>
    /// 首次施加时登记施加者，并按本次施加量扣减力量。
    /// 注意：PowerModel.AfterApplied 只在 Power 首次附着时触发，目标已持有该 Power 时再次施加
    /// 只会走 PowerCmd.ModifyAmount 叠加层数，不会再次触发 AfterApplied。
    /// 因此这里改用 AfterPowerAmountChanged：首次施加与后续每次叠加都会调用，
    /// 且 amount 参数就是本次的增减量（首次为施加量，叠加为增量）。
    /// </summary>
    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power != this) return;
        // 只结算正向叠加（即"再次施加"）；层数被消耗/移除时不重复扣力量。
        if (amount <= 0m) return;

        if (applier != null)
        {
            ((StringVar)base.DynamicVars["Applier"]).StringValue = applier.Name;
        }

        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Owner, -amount, null, null);
    }
}