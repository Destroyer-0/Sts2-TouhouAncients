using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.cardTags;
using TouhouAncients.Scripts.Hooks;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.powers;

/// <summary>气氛：数值即当前气氛，达到阈值时由 <see cref="KoishiPerformanceCmd"/> 进入嗨到爆。</summary>
public sealed class AtmospherePower : TouhouAncientPowerModel
{
    private const string ThresholdKey = "Threshold";

    // 氛围计数不该被人工制品挡下或被增益放大
    public override PowerType Type => PowerType.None;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar(ThresholdKey, KoishiPerformanceCmd.BaseThreshold)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromKeyword(TouhouAncientKeywords.TouhouAncientHyped)];

    /// <summary>门票加高阈值后刷新文案里的数字。</summary>
    public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount,
        Creature? applier, CardModel? cardSource)
    {
        if (power != this) return Task.CompletedTask;

        if (base.Owner.Player is { } player)
        {
            base.DynamicVars[ThresholdKey].BaseValue = KoishiPerformanceCmd.GetThreshold(player);
        }

        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }
}
