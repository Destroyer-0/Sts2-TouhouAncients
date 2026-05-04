using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Enchantments;
using TouhouAncients.Scripts.Enchantment;

namespace TouhouAncients.Scripts.relics;

public class Bileihaopaiduozhua : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new StringVar("EnchantmentName", ModelDb.Enchantment<HighQuality>().Title.GetFormattedText())];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromCardWithCardHoverTips<Barricade>()
        .Concat(HoverTipFactory.FromEnchantment<HighQuality>());

    public override async Task AfterObtained()
    {
        List<CardPileAddResult> results = new List<CardPileAddResult>();
        for (int i = 0; i < 4; i++)
        {
            CardModel card = base.Owner.RunState.CreateCard(ModelDb.Card<Barricade>(), base.Owner);
            CardCmd.Enchant<HighQuality>(card, 1m);
			results.Add(await CardPileCmd.Add(card, PileType.Deck));
        }
        CardCmd.PreviewCardPileAdd(results, 2f);
    }
}