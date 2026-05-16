using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TouhouAncients.Scripts.Enchantment;

/// <summary>
/// 诈术：打出后，获得1层缓冲，这张牌的费用在本场战斗中增加1。
/// 只能附魔在耗能至少为1的卡牌上。
/// </summary>
public class Trick : CustomEnchantmentModel
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
    [
        HoverTipFactory.FromPower<BufferPower>()
    ];

    public override bool CanEnchant(CardModel card)
    {
        if (card.EnergyCost.CostsX) return false;
        return card.EnergyCost.Canonical >= 1;
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        if (cardPlay?.Card != base.Card) return;
        if (base.Status != EnchantmentStatus.Normal) return;

        await PowerCmd.Apply<BufferPower>(base.Card.Owner.Creature, 1m, base.Card.Owner.Creature, base.Card);
        base.Card.EnergyCost.AddThisCombat(1);
    }
}
