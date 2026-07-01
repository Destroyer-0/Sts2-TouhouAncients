using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.RelicPools;
using TouhouAncients.Scripts.cards;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 弹幕的地狱 — 拾起时，将1张纯粹的弹幕地狱加入牌组。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class HellOfBullets : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<PureHellOfBullets>();

    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        var player = base.Owner;
        var card = player.RunState.CreateCard(ModelDb.Card<PureHellOfBullets>(), player);
        var result = await CardPileCmd.Add(card, PileType.Deck);
        CardCmd.PreviewCardPileAdd([result], 2f);
    }
}
