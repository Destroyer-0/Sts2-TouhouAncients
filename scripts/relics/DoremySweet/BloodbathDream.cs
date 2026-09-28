using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.relics.DoremySweet;

/// <summary>
/// 浴血之梦：如果你在本回合中没有打出过攻击牌，则失去 {HpLoss} 点生命。
/// 拾起时，将 {Cards} 张撕咬加入你的牌组。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class BloodbathDream : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new HpLossVar(4m),
        new CardsVar(2)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<Maul>();

    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        var player = base.Owner;
        if (player?.Creature == null) return;

        var cardCount = base.DynamicVars.Cards.IntValue;
        if (cardCount <= 0) return;

        Flash();

        var results = new List<CardPileAddResult>();

        for (int i = 0; i < cardCount; i++)
        {
            var maul = player.RunState.CreateCard<Maul>(player);
            results.Add(await CardPileCmd.Add(maul, PileType.Deck));
        }

        CardCmd.PreviewCardPileAdd(results, 2f);
    }

    /// <summary>
    /// 回合结束时结算：本回合一张攻击牌都没有打出过，就失去生命（不可格挡、不受能力加成）。
    /// 判定方式与参照原版遗物「波纹水盆」（RippleBasin）的回合结束判定一致。
    /// </summary>
    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != base.Owner.Creature.Side) return;
        if (!participants.Contains(base.Owner.Creature)) return;
        if (base.Owner.Creature.CombatState == null) return;

        bool playedAttackThisTurn = CombatManager.Instance.History.CardPlaysFinished.Any(
            (CardPlayFinishedEntry e) =>
                e.HappenedThisTurn(base.Owner.Creature.CombatState) &&
                e.CardPlay.Card.Type == CardType.Attack &&
                e.CardPlay.Player == base.Owner);

        if (playedAttackThisTurn) return;

        Flash();
        await CreatureCmd.Damage(
            choiceContext,
            base.Owner.Creature,
            base.DynamicVars.HpLoss.BaseValue,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);
    }
}
