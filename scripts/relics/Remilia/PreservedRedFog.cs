using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

[Pool(typeof(SharedRelicPool))]
public class PreservedRedFog : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromCardWithCardHoverTips<Folly>();

    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        var player = base.Owner;
        int count = base.DynamicVars.Cards.IntValue;

        // 从牌组中选择 count 张牌进行复制
        foreach (CardModel cardModel in await CardSelectCmd.FromDeckGeneric(
                     prefs: new CardSelectorPrefs(base.SelectionScreenPrompt, count),
                     player: player,
                     filter: static c => c.Type != CardType.Quest))
        {
            CardModel cloned = player.RunState.CloneCard(cardModel);
            CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(cloned, PileType.Deck));
        }

        // 将一张愚行加入牌组
        await CardPileCmd.AddCurseToDeck<Folly>(player);
    }
}
