using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using TouhouAncients.Scripts.cards;

namespace TouhouAncients.Scripts.relics.DoremySweet;

/// <summary>
/// 启示之梦：拾起时，将一张幻梦无垠加入牌组。
/// 休息时的「幻梦变化」由卡牌自身的 TryModifyRestSiteHealRewards 提供，遗物不重复实现。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class RevelationDream : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        var card = base.Owner.RunState.CreateCard<IllusoryDreamWhisper>(base.Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck), 2f);
    }
}
