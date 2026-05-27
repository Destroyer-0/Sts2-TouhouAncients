using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using TouhouAncients.Scripts.cards;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 博丽御币：拾起时，将一张梦想封印加入牌组。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class HakureiGohei : TouhouAncientRelics
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromCardWithCardHoverTips<DreamSeal>();

    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        CardModel card = base.Owner.RunState.CreateCard<DreamSeal>(base.Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck), 2f);
    }
}
