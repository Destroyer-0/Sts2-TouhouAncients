using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

[Pool(typeof(SharedRelicPool))]
public class Baibaixiangxiangruanruan : TouhouAncientRelics
{
    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        // 删除 4 张牌
        foreach (var card in await CardSelectCmd.FromDeckForRemoval(
                     prefs: new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 4),
                     player: base.Owner))
        {
            await CardPileCmd.RemoveFromDeck(card);
        }

        // 复制牌组中所有防御牌并升级
        var defends = PileType.Deck.GetPile(base.Owner).Cards
            .Where(c => c.Tags.Contains(CardTag.Defend))
            .ToList();

        
        List<CardPileAddResult> results = new List<CardPileAddResult>();
        
        
        foreach (var defend in defends)
        {
            var cloned = base.Owner.RunState.CloneCard(defend);
            CardCmd.Upgrade(cloned);
            results.Add(await CardPileCmd.Add(cloned, PileType.Deck));
            await CardPileCmd.Add(cloned, PileType.Deck);
        }
        
        CardCmd.PreviewCardPileAdd(results, 2f);
    }
}