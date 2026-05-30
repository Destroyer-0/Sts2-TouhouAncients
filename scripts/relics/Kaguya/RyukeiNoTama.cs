using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 龙颈之玉：
/// 你每打出5张牌，将一张带有虚无、辉星费用为0的七星放入手牌。此计数跨战斗保留。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class RyukeiNoTama : TouhouAncientRelics
{
    /// <summary>
    /// 跨战斗保留的打出计数，每5张触发一次七星。
    /// </summary>
    [SavedProperty] private int TouhouAncients_CardsPlayedCounter { get; set; }
    
    public override int DisplayAmount => TouhouAncients_CardsPlayedCounter;
    public override bool ShowCounter => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Card", 5)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<SevenStars>().Append(HoverTipFactory.FromKeyword(CardKeyword.Ethereal));

    /// <summary>
    /// 每打出5张牌，将一张辉星费用为0的七星（带虚无）放入手牌。
    /// </summary>
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = base.Owner;
        if (cardPlay.Card.Owner != player) return;
        if (player.Creature.CombatState == null) return;

        TouhouAncients_CardsPlayedCounter++;
        InvokeDisplayAmountChanged();

        if (TouhouAncients_CardsPlayedCounter >= 5)
        {
            TouhouAncients_CardsPlayedCounter = 0;
            InvokeDisplayAmountChanged();
            Flash();

            // 创建七星牌，辉星费用设为0，添加虚无关键字
            var card = player.Creature.CombatState.CreateCard<SevenStars>(player);
            card.SetStarCostThisCombat(0);
            if (!card.Keywords.Contains(CardKeyword.Ethereal))
            {
                CardCmd.ApplyKeyword(card, CardKeyword.Ethereal);
            }

            // 放入手牌
            CardCmd.PreviewCardPileAdd(
                await CardPileCmd.AddGeneratedCardsToCombat([card], PileType.Hand, addedByPlayer: true, CardPilePosition.Random));
        }
    }
}
