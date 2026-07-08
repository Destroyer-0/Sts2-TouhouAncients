using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 溢出的暇秽 — 在每个回合开始时获得2能量，额外抽两张牌。
/// 回合结束时，向抽牌堆加入等同于剩余手牌数量张碎屑、等同于剩余能量数量张虚空。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class OverflowingDefilement : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars => 
    [
        new EnergyVar(2),
        new CardsVar(2)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<Debris>().Concat(HoverTipFactory.FromCardWithCardHoverTips<MegaCrit.Sts2.Core.Models.Cards.Void>());

    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        if (player != base.Owner)
            return amount;
        return amount + base.DynamicVars.Energy.IntValue;
    }
    
    // /// <summary>
    // /// 回合开始时获得2能量，额外抽两张牌
    // /// </summary>
    // public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    // {
    //     if (side != base.Owner.Creature.Side) return;
    //
    //     Flash();
    //     // 额外抽两张牌
    //     await CardPileCmd.Draw(new ThrowingPlayerChoiceContext(), DynamicVars.Cards.IntValue, base.Owner, fromHandDraw: true);
    // }

    public override decimal ModifyHandDraw(Player player, decimal count)
    {
        if (player != base.Owner)
        {
            return count;
        }

        return count + DynamicVars.Cards.IntValue;
    }

    /// <summary>
    /// 回合结束时向抽牌堆加入碎屑和虚空
    /// </summary>
    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != base.Owner.Creature.Side) return;
        if (base.Owner.Creature.CombatState == null) return;

        var player = base.Owner;
        var hand = PileType.Hand.GetPile(player).Cards;
        var remainingEnergy = player.PlayerCombatState.Energy;

        int debrisCount = hand.Count(x => x.Type != CardType.Status);
        int voidCount = (int)Math.Floor((decimal)remainingEnergy);

        if (debrisCount <= 0 && voidCount <= 0) return;

        Flash();
        List<CardPileAddResult> cardPileAddResults = new();
        // 加入碎屑 = 剩余手牌数
        for (int i = 0; i < debrisCount; i++)
        {
            var debris = player.Creature.CombatState.CreateCard<Debris>(player);
            cardPileAddResults.Add(await CardPileCmd.Add(debris, PileType.Draw,CardPilePosition.Random));
        }

        // 加入虚空 = 剩余能量数（使用全局限定名称避免与 System.Void 冲突）
        for (int i = 0; i < voidCount; i++)
        {
            var voidCard = player.Creature.CombatState.CreateCard<MegaCrit.Sts2.Core.Models.Cards.Void>(player);
            cardPileAddResults.Add(await CardPileCmd.Add(voidCard, PileType.Draw, CardPilePosition.Random));
        }

        CardCmd.PreviewCardPileAdd(cardPileAddResults);
    }

    public override Task AfterCombatEnd(MegaCrit.Sts2.Core.Rooms.CombatRoom _)
    {
        return Task.CompletedTask;
    }
}
