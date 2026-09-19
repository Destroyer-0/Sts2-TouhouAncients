using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;
using TouhouAncients.Scripts.cards;

namespace TouhouAncients.Scripts.relics.DoremySweet;

/// <summary>
/// 浴血之梦：拾起时，将1张疼痛与1张撕咬加入你的牌组。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class BloodbathDream : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Curses", 1),
        new CardsVar(2)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        ..HoverTipFactory.FromCardWithCardHoverTips<PainCurse>(),
        ..HoverTipFactory.FromCardWithCardHoverTips<Maul>()
    ];

    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        var player = base.Owner;
        if (player?.Creature == null) return;

        var curseCount = base.DynamicVars["Curses"].IntValue;
        var cardCount = base.DynamicVars.Cards.IntValue;

        if (curseCount <= 0 && cardCount <= 0) return;

        Flash();

        var results = new List<CardPileAddResult>();

        for (int i = 0; i < curseCount; i++)
        {
            var curse = player.RunState.CreateCard<PainCurse>(player);
            results.Add(await CardPileCmd.Add(curse, PileType.Deck));
        }

        for (int i = 0; i < cardCount; i++)
        {
            var maul = player.RunState.CreateCard<Maul>(player);
            results.Add(await CardPileCmd.Add(maul, PileType.Deck));
        }

        CardCmd.PreviewCardPileAdd(results, 2f);
    }
}
