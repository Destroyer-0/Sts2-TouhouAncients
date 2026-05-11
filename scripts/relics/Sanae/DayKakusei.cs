using System.Linq;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.RelicPools;

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

    /// <summary>
    /// 当你使用药水时，生成一张随机能力牌，其耗能降至0且拥有虚无。
    /// 参考 WhiteNoise。
    /// </summary>
    public override async Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        if (potion.Owner != base.Owner) return;

        Flash();

        // 生成一张随机能力牌
        var card = CardFactory.GetDistinctForCombat(base.Owner,
            from c in base.Owner.Character.CardPool.GetUnlockedCards(
                base.Owner.UnlockState, base.Owner.RunState.CardMultiplayerConstraint)
            where c.Type == CardType.Power
            select c, 1, base.Owner.RunState.Rng.CombatCardGeneration
        ).FirstOrDefault();

        if (card != null)
        {
            // 耗能降至0（本回合）
            card.SetToFreeThisTurn();
            // 添加虚无
            CardCmd.ApplyKeyword(card, CardKeyword.Ethereal);
            // 加入手牌
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, addedByPlayer: true);
        }
    }
}
