using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using TouhouAncients.Scripts.cards;

namespace TouhouAncients.Scripts.relics.IdleKoishi;

/// <summary>某人的画像：拾起时将一张「献给你的荆棘之歌」加入牌组。</summary>
[Pool(typeof(SharedRelicPool))]
public class PortraitOfSomeone : TouhouAncientRelics
{
    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        var player = base.Owner;
        if (player == null) return;

        Flash();
        var card = player.RunState.CreateCard(ModelDb.Card<ThornSongForYou>(), player);
        List<CardPileAddResult> results = [await CardPileCmd.Add(card, PileType.Deck)];
        CardCmd.PreviewCardPileAdd(results, 2f);
    }
}
