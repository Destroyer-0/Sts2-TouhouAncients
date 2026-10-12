using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace TouhouAncients.Scripts.Enchantment;

/// <summary>
/// 亲笔：这张卡牌从牌组中被移除时，获得100金币。
/// </summary>
public class Autograph : TouhouAncientEnchantmentModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Gold", 60m)];

    /// <summary>BeforeCardRemoved 只在 CardPileCmd.RemoveFromDeck 里触发，正合「从牌组中被移除」。</summary>
    public override async Task BeforeCardRemoved(CardModel card)
    {
        if (card != base.Card) return;
        if (card.Owner == null) return;

        await PlayerCmd.GainGold(base.DynamicVars["Gold"].BaseValue, card.Owner);
    }
}
