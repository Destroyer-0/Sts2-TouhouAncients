using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TouhouAncients.Scripts.cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 诈术师：玩家打出的非「幸运」戏法牌耗能不匹配时，玩家受到伤害且该牌造成的伤害减半。
/// 目标费用直接存在 Amount（0~3，初始 1），只能由 InabaTewiMonster 用 SetAmount 改写——
/// PowerCmd.ModifyAmount 在 Amount == 0 时会走 ShouldRemoveDueToAmount 把本能力整个移除。
/// </summary>
public class TricksterPower : TouhouAncientPowerModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Damage", 1m),
        new DynamicVar("DamageReduction", 50m)
    ];

    private sealed class Data
    {
        public readonly Dictionary<CardPlay, bool> MismatchedPlays = new();
    }

    protected override object InitInternalData() => new Data();

    private bool HasMismatchedCost(CardModel card, CardPlay? cardPlay)
    {
        if (cardPlay != null && GetInternalData<Data>().MismatchedPlays.TryGetValue(cardPlay, out bool mismatch))
            return mismatch;
        return card.EnergyCost.GetResolved() != Amount;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (cardSource == null || cardSource is LuckyTrick || cardSource.Owner?.Creature.IsPlayer != true) return 1m;

        // GetResolved 已把普通牌耗能 clamp 到 ≥0，X 费牌取打出时捕获的 X 值
        return HasMismatchedCost(cardSource, cardPlay)
            ? 1m - base.DynamicVars["DamageReduction"].BaseValue / 100m
            : 1m;
    }

    public void ResetAmount(int amount)
    {
        SetAmount(amount,true);
        Flash();
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        CardModel card = cardPlay.Card;
        if (card is not LuckyTrick && card.Owner?.Creature.IsPlayer == true)
            GetInternalData<Data>().MismatchedPlays[cardPlay] = card.EnergyCost.GetResolved() != Amount;
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!GetInternalData<Data>().MismatchedPlays.Remove(cardPlay, out bool mismatch) || !mismatch) return;
        CardModel card = cardPlay.Card;

        await CreatureCmd.Damage(choiceContext, card.Owner.Creature, base.DynamicVars["Damage"].BaseValue,
            ValueProp.Unpowered | ValueProp.Move, null, null);
    }
}
