using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TouhouAncients.Scripts.cardTags;
using TouhouAncients.Scripts.Hooks;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 渐强：打出牌时按实际耗能获得气氛（至少 1），耗能高于上一张牌时额外 +1。
/// </summary>
public sealed class CrescendoPower : TouhouAncientPowerModel
{
    private const int MinAtmosphere = 1;
    private const int BonusAtmosphere = 1;

    private int? _lastEnergySpent;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("MinAtmosphere", MinAtmosphere),
        new DynamicVar("BonusAtmosphere", BonusAtmosphere),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromKeyword(TouhouAncientKeywords.TouhouAncientHyped)];

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (base.Owner.Player is not { } player) return;
        if (cardPlay.Card.Owner != player) return;
        // 额外打出（Replay）只算一次
        if (!cardPlay.IsFirstInSeries) return;

        var energySpent = cardPlay.Resources.EnergySpent;

        var gain = Math.Max(MinAtmosphere, energySpent);
        if (_lastEnergySpent is { } last && energySpent > last) gain += BonusAtmosphere;
        _lastEnergySpent = energySpent;

        await KoishiPerformanceCmd.GainAtmosphere(choiceContext, player, gain);
    }
}
