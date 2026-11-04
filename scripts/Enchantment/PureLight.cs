using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TouhouAncients.Scripts.Enchantment;

/// <summary>
/// 纯粹：打出后，清除所有手牌的附魔并全部附魔纯粹。
/// </summary>
public class PureLight : TouhouAncientEnchantmentModel
{
    // 没有额外的卡面短文本，true 会去找 .extraCardText 键
    public override bool HasExtraCardText => false;

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        if (cardPlay?.Card != base.Card) return;

        var hand = base.Card.Owner.PlayerCombatState?.Hand;
        if (hand == null) return;

        // 复制一份，避免附魔过程中改动集合
        foreach (var card in hand.Cards.ToList())
        {
            // 本牌已在打出流程里，再附魔会干扰结算
            if (card == base.Card) continue;
            if (card.Enchantment is PureLight) continue;

            // Enchant 遇到异种附魔会抛异常，必须先清
            if (card.Enchantment != null) CardCmd.ClearEnchantment(card);

            if (ModelDb.Enchantment<PureLight>().CanEnchant(card))
            {
                CardCmd.Enchant<PureLight>(card, 1m);
            }
        }
    }
}
