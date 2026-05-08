using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using TouhouAncients.Scripts.Enchantment;

namespace TouhouAncients.Scripts.relics;

[Pool(typeof(SharedRelicPool))]
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

    /// <summary>
    /// 每次卡牌奖励生成时，已有同名牌存在于牌库的卡牌被附魔：优质。
    /// </summary>
    public override bool TryModifyCardRewardOptionsLate(Player player, List<CardCreationResult> cardRewards, CardCreationOptions options)
    {
        if (player != base.Owner) return false;
        EnchantValidCards(cardRewards);
        return true;
    }

    public override void ModifyMerchantCardCreationResults(Player player, List<CardCreationResult> cards)
    {
        if (player == base.Owner)
            EnchantValidCards(cards);
    }

    public override bool TryModifyCardBeingAddedToDeck(CardModel card, out CardModel? newCard)
    {
        newCard = null;
        if (card.Owner != base.Owner) return false;
        if (card.Enchantment != null) return false;

        var highQuality = ModelDb.Enchantment<HighQuality>();
        if (!highQuality.CanEnchant(card)) return false;
        if (!Owner.Deck.Cards.Any(x => x.Id.Entry == card.Id.Entry)) return false;

        newCard = EnchantCard(card);
        return true;
    }

    private void EnchantValidCards(List<CardCreationResult> options)
    {
        var highQuality = ModelDb.Enchantment<HighQuality>();
        foreach (var option in options)
        {
            var card = option.Card;
            if (!highQuality.CanEnchant(card)) continue;
            if (card.Enchantment != null) continue;
            if (!Owner.Deck.Cards.Any(x => x.Id.Entry == card.Id.Entry)) continue;

            option.ModifyCard(EnchantCard(card), this);
        }
    }

    private CardModel EnchantCard(CardModel card)
    {
        var enchanted = base.Owner.RunState.CloneCard(card);
        CardCmd.Enchant<HighQuality>(enchanted, 1m);
        return enchanted;
    }
}