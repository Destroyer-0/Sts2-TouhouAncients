using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using TouhouAncients.Scripts.cards;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 绯想之剑：拾起时，将一张全人类的绯想天加入牌组。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class HisouSword : TouhouAncientRelics
{
    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<AllCreationHisou>()
    ];

    public override async Task AfterObtained()
    {
        // var player = base.Owner;
        //
        // var card = await CardSelectCmd.FromChooseACardScreen(new BlockingPlayerChoiceContext(), new List<CardModel>()
        // {
        //     player.RunState.CreateCard(ModelDb.Card<AllCreationHisou>(),player),
        //     player.RunState.CreateCard(ModelDb.Card<AllCreationHisou_Old>(),player)
        // }, Owner, false);
        // if (card != null)
        // {
        //     CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck));
        // }
        //
        
        CardModel card = base.Owner.RunState.CreateCard<AllCreationHisou>(base.Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck), 2f);
        
        // var cardModel = ModelDb.Card<AllCreationHisou>();
        // var card = player.RunState.CreateCard(cardModel, player);
        // var addResult = await CardPileCmd.Add(card, PileType.Deck);
        // CardCmd.PreviewCardPileAdd(addResult);
    }
}
