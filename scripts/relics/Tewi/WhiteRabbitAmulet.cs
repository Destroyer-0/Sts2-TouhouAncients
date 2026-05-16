using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 白兔御守：拾起时移除牌组中所有诅咒，之后阻止诅咒加入牌组。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class WhiteRabbitAmulet : TouhouAncientRelics
{
    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        var deck = PileType.Deck.GetPile(base.Owner);
        var curses = deck.Cards.Where(c => c is { Type: CardType.Curse, IsRemovable: true }).ToList();
        if (curses.Count > 0)
        {
            Flash();
            await CardPileCmd.RemoveFromDeck(curses);
        }
    }

    // 阻止诅咒加入牌组
    public override bool ShouldAddToDeck(CardModel card)
    {
        if (card.Owner != base.Owner) return true;
        if (card.Type == CardType.Curse) return false;
        return true;
    }
}
