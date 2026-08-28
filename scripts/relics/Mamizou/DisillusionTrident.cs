using System.Collections.Generic;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using TouhouAncients.Scripts.cards;
using TouhouAncients.Scripts.potions;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 幻灭三叉戟：拾起时，将{Cards}张未名妖魔加入你的牌组。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class DisillusionTrident : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(1),
    ];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromCardWithCardHoverTips<NamelessYoukai>();
    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        CardModel card = base.Owner.RunState.CreateCard<NamelessYoukai>(base.Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck), 2f);
    }
}
