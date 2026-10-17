using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TouhouAncients.Scripts.Enchantment;

/// <summary>
/// 回响：打出此牌后，将一张本回合费用 +1 的复制品加入你的手牌。
/// 复制品会保留本附魔，故可连锁，费用逐次递增。
/// </summary>
public class Echo : TouhouAncientEnchantmentModel
{
    public override bool HasExtraCardText => true;

    // X 费不吃本地加减费，+1 没有效果
    public override bool CanEnchant(CardModel card)
    {
        if (card.EnergyCost.CostsX)
        {
            return false;
        }

        return base.CanEnchant(card);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!HasCard) return;
        if (cardPlay.Card != Card) return;

        var copy = cardPlay.Card.CreateClone();
        copy.EnergyCost.AddThisTurnOrUntilPlayed(1);
        await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, Card.Owner);
    }
}
