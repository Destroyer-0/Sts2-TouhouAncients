using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using TouhouAncients.Scripts.cards;

namespace TouhouAncients.Scripts.relics.DoremySweet;

/// <summary>
/// 启示之梦：拾起时，将一张幻梦呢喃加入牌组（该牌自带休息时的「梦醒时」变化效果）。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class RevelationDream : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromCardWithCardHoverTips<IllusoryDreamWhisper>();

    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        CardModel card = base.Owner.RunState.CreateCard<IllusoryDreamWhisper>(base.Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck), 2f);
    }
}
