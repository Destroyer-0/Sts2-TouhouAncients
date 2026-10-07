using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace TouhouAncients.Scripts.Afflictions;

/// <summary>
/// 魔力躁动：这张牌若不因打出而进入弃牌堆或消耗堆，则转变为叛乱之潮。
/// </summary>
public sealed class ManaRestlessness : TouhouAncientAfflictionModel
{
    public override bool CanAfflict(CardModel card)
    {
        return card.Rarity == CardRarity.Basic&& (card.Tags.Contains(CardTag.Strike)||card.Tags.Contains(CardTag.Defend));
    }

    public override bool HasExtraCardText => true;

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        if (card != Card) return;
        // 因打出而离开：不转变
        if (oldPileType == PileType.Play) return;
        if (card.Pile?.Type is not (PileType.Discard or PileType.Exhaust)) return;

        // 一张牌同时只能挂一种侵蚀：必须先清自身，异类型叠加会抛异常
        CardModel target = Card;
        target.ClearAfflictionInternal();
        await CardCmd.Afflict<RebellionTide>(target, 1m);
    }
}
