using System.Linq;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace TouhouAncients.Scripts.relics;

[Pool(typeof(SharedRelicPool))]
public class DayKakusei : TouhouAncientRelics
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPotion<PowerPotion>()];

    /// <summary>
    /// 战斗开始时，获得一瓶能力药水。
    /// </summary>
    public override async Task BeforeCombatStartLate()
    {
        Flash();
        await PotionCmd.TryToProcure<PowerPotion>(base.Owner);
    }
    
    // /// <summary>
    // /// 使用能力药水时，抽一张牌。
    // /// </summary>
    // public override async Task AfterPotionUsed(PotionModel potion, Creature? target)
    // {
    //     if (potion.Owner != base.Owner) return;
    //     if (potion.Id.Entry != "POWER_POTION") return;
    //
    //     Flash();
    //     await CardPileCmd.Draw(new ThrowingPlayerChoiceContext(), 1m, base.Owner, fromHandDraw: true);
    // }

    /// <summary>
    /// 四版：升级你在战斗中生成的能力牌。
    /// </summary>
    /// <param name="card"></param>
    /// <param name="addedByPlayer"></param>
    /// <returns></returns>
    public override Task AfterCardGeneratedForCombat(CardModel card, bool addedByPlayer)
    {
        if (card.Owner != base.Owner) return Task.CompletedTask;
        if (addedByPlayer&&card.Type is CardType.Power)
        {
            CardCmd.Upgrade(card);
        }
        return Task.CompletedTask;
    }

    // /// <summary>
    // /// 当你使用药水时，生成一张随机能力牌，其耗能降至0且拥有虚无。
    // //废弃。
    // //废弃原因：麻烦
    // /// 参考 WhiteNoise。
    // /// </summary>
    // public override async Task AfterPotionUsed(PotionModel potion, Creature? target)
    // {
    //     if (potion.Owner != base.Owner) return;
    //
    //     Flash();
    //
    //     // 生成一张随机能力牌
    //     var card = CardFactory.GetDistinctForCombat(base.Owner,
    //         from c in base.Owner.Character.CardPool.GetUnlockedCards(
    //             base.Owner.UnlockState, base.Owner.RunState.CardMultiplayerConstraint)
    //         where c.Type == CardType.Power
    //         select c, 1, base.Owner.RunState.Rng.CombatCardGeneration
    //     ).FirstOrDefault();
    //
    //     if (card != null)
    //     {
    //         // 耗能降至0（本回合）
    //         card.SetToFreeThisTurn();
    //         // 添加虚无
    //         CardCmd.ApplyKeyword(card, CardKeyword.Ethereal);
    //         // 加入手牌
    //         await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, addedByPlayer: true);
    //     }
    // }
    
    //三版：你通过能力药水使用的能力牌将出现在额外的卡牌奖励中。
    //废弃。
    //废弃原因：代码实现不出来
    // public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    // {
    //     if (cardPlay.Card.Owner != base.Owner || !CombatManager.Instance.IsInProgress)
    //         return;
    //     if (cardPlay.Card.CombatState == null) return;
    //     if(cardPlay.Card.)
    //
    //     AbstractRoom currentRoom = cardPlay.Card.CombatState.RunState.CurrentRoom;
    //     if (currentRoom is CombatRoom combatRoom)
    //     {
    //         combatRoom.AddExtraReward(base.Owner,
    //             new CardReward([cardPlay.Card], CardCreationSource.Other, base.Owner));
    //     }
    //
    //     return;
    // }
}
