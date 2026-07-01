using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using TouhouAncients.Scripts.cards;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 暴食之牙：拾起时，将一张狂飨加入牌组。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class GluttonousFang : TouhouAncientRelics
{
    public override string DefaultFileName => "yuuma_default";
    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<TheFeast>();

    public override async Task AfterObtained()
    {
        var player = base.Owner;
        var card = player.RunState.CreateCard<TheFeast>(player);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck), 2f);
    }
}
