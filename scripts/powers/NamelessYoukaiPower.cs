using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 未名妖魔（回合内 Power，参照原版独白 MonologuePower）
/// - 本回合内，你打出与上一张打出的牌颜色（卡牌池）不同的牌时，获得1能量并在本回合获得力量
/// - 用内部 Data 记录上一张打出的牌；BeforeCardPlayed 记录当前牌，AfterCardPlayed 比较颜色并触发
/// - 回合结束时移除自身，并扣除本回合累计获得的力量（临时力量）
/// </summary>
public class NamelessYoukaiPower : TouhouAncientPowerModel
{
    private class Data
    {
        /// <summary>当前正要打出的牌（BeforeCardPlayed 记录，AfterCardPlayed 消费）</summary>
        public CardModel? currentCard;

        /// <summary>上一张打出的牌（用于比较颜色）</summary>
        public CardModel? previousCard;
    }

    /// <summary>累计已获得的力量（用于回合结束时扣除与 UI 显示）</summary>
    public const string StrengthAppliedKey = "StrengthApplied";

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType
    {
        get
        {
            if (base.DynamicVars[StrengthAppliedKey].IntValue != 0)
            {
                return PowerStackType.Counter;
            }
            return PowerStackType.None;
        }
    }

    public override int DisplayAmount => base.DynamicVars[StrengthAppliedKey].IntValue;

    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<StrengthPower>(1m),
        new DynamicVar(StrengthAppliedKey, 0m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<StrengthPower>()];

    protected override object InitInternalData()
    {
        return new Data();
    }

    /// <summary>
    /// 上一张打出的牌（用于发光提示：手牌中与它颜色不同的牌 ShouldGlowGold）。
    /// </summary>
    public CardModel? PreviousCard => GetInternalData<Data>().previousCard;

    /// <summary>
    /// 设定初始的"上一张牌"为未名妖魔自身，使打出后的下一张牌即可与它比较颜色。
    /// </summary>
    public void SetInitialPreviousCard(CardModel card)
    {
        AssertMutable();
        GetInternalData<Data>().previousCard = card;
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature != base.Owner)
        {
            return Task.CompletedTask;
        }
        GetInternalData<Data>().currentCard = cardPlay.Card;
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var data = GetInternalData<Data>();
        var current = data.currentCard;
        data.currentCard = null;
        if (current == null) return;

        // 更新"上一张打出的牌"为本次打出的牌
        var previous = data.previousCard;
        data.previousCard = current;

        // 第一张牌或与上一张颜色（卡池）相同则不触发
        if (previous == null) return;
        if (current.Pool == previous.Pool) return;

        Flash();
        // 获得1能量
        await PlayerCmd.GainEnergy(1, base.Owner.Player!);
        // 本回合获得力量
        await PowerCmd.Apply<StrengthPower>(choiceContext, base.Owner, base.DynamicVars.Strength.IntValue, base.Owner, null, silent: true);
        base.DynamicVars[StrengthAppliedKey].BaseValue += base.DynamicVars.Strength.IntValue;
        InvokeDisplayAmountChanged();
    }

    /// <summary>
    /// 回合结束时移除自身，并扣除本回合累计获得的力量。
    /// </summary>
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(base.Owner))
        {
            await PowerCmd.Remove(this);
            await PowerCmd.Apply<StrengthPower>(choiceContext, base.Owner, -base.DynamicVars[StrengthAppliedKey].BaseValue, base.Owner, null, silent: true);
        }
    }
}
