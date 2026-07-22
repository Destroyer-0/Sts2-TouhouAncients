using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 不幸 — 紫苑的被动能力。
/// 受到攻击牌攻击时，攻击来源失去 5 层王国资产并叠加 2 层灾厄。
/// 每张攻击牌每次打出只触发一次（仿照 RupturePower 的卡牌追踪机制）。
/// </summary>
public class UnfortunatePower : TouhouAncientPowerModel
{
    private class Data
    {
        public readonly Dictionary<CardModel, bool> triggeredCards = new Dictionary<CardModel, bool>();
    }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        // 不追踪紫苑自己打出的牌
        if (cardPlay.Card.Owner.Creature == base.Owner)
        {
            return Task.CompletedTask;
        }
        // 只追踪攻击牌
        if (cardPlay.Card.CardType != CardType.Attack)
        {
            return Task.CompletedTask;
        }
        GetInternalData<Data>().triggeredCards.Add(cardPlay.Card, false);
        return Task.CompletedTask;
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        if (target != base.Owner) return;
        if (cardSource == null) return;
        Data data = GetInternalData<Data>();
        if (!data.triggeredCards.TryGetValue(cardSource, out bool alreadyTriggered)) return;
        if (alreadyTriggered) return;
        if (dealer == null) return;

        // 标记已触发，同一张牌本次打出不再触发
        data.triggeredCards[cardSource] = true;

        Flash();
        // 来源失去 5 层王国资产
        if (dealer is Player)
        {
            RoyaltiesPower royalties = dealer.GetPower<RoyaltiesPower>();
            if (royalties != null && royalties.Amount > 0)
            {
                decimal lossAmount = System.Math.Min(5m, royalties.Amount);
                await PowerCmd.Apply<RoyaltiesPower>(choiceContext, dealer, -lossAmount, base.Owner, null);
            }
        }
        // 来源叠加 2 层灾厄
        await PowerCmd.Apply<DoomPower>(choiceContext, dealer, 2m, base.Owner, null);
    }

    public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        GetInternalData<Data>().triggeredCards.Remove(cardPlay.Card);
        return Task.CompletedTask;
    }
}
