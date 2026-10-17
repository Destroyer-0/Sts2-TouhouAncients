using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 「好奇」：玩家每合计打出「联机人数」张能力牌，贝子获得力量。
/// 图标数字保持力量收获量；剩余张数只在 smartDescription 里显示。
/// </summary>
public sealed class CuriosityPower : TouhouAncientPowerModel
{
    private const string CardsNeededKey = "CardsNeeded";

    private const string RemainingKey = "Remaining";

    private int _playsSinceTrigger;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(CardsNeededKey, 1),
        new DynamicVar(RemainingKey, 1)
    ];

    public void Configure(int playerCount)
    {
        _playsSinceTrigger = 0;
        base.DynamicVars[CardsNeededKey].BaseValue = playerCount;
        RefreshRemaining(playerCount);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Type != CardType.Power || base.Owner.IsDead) return;

        int needed = Math.Max(1, base.DynamicVars[CardsNeededKey].IntValue);
        _playsSinceTrigger++;
        if (_playsSinceTrigger >= needed)
        {
            _playsSinceTrigger = 0;
            Flash();
            await PowerCmd.Apply<StrengthPower>(choiceContext, base.Owner, base.Amount, base.Owner, null);
        }

        RefreshRemaining(needed);
    }

    private void RefreshRemaining(int needed)
    {
        base.DynamicVars[RemainingKey].BaseValue = Math.Max(0, needed - _playsSinceTrigger);
        InvokeDisplayAmountChanged();
    }
}
