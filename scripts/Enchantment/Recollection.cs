using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TouhouAncients.Scripts.Enchantment;

/// <summary>
/// 回忆：首次进入弃牌堆时（非正常打出后），将其自动打出。
/// </summary>
public class Recollection : CustomEnchantmentModel
{
    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        if (!HasCard) return;
        if (card != base.Card) return;
        if (card.Pile == null) return;

        // 进入弃牌堆（如从手牌被弃）→ 自动打出
        if (card.Pile?.Type == PileType.Discard)
        {
            await CardCmd.AutoPlay(
                new ThrowingPlayerChoiceContext(),
                card,
                target: null);
            base.Status = EnchantmentStatus.Disabled;
        }
    }
}
