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

        var highQuality = ModelDb.Enchantment<HighQuality>();
        bool modified = false;

        foreach (var reward in cardRewards)
        {
            var card = reward.Card;

            // 跳过不符合附魔条件、或已有同名牌不在牌库的卡牌
            if (!highQuality.CanEnchant(card)) continue;
            if (Owner.Deck.Cards.Any(x => x.Id.Entry == card.Id.Entry))
            {
                // 每张卡只能有一个附魔，跳过已有其他附魔的卡牌
                if (card.Enchantment != null) continue;

                // 克隆卡牌 → 附魔 → 替换奖励卡
                var enchantedCard = base.Owner.RunState.CloneCard(card);
                CardCmd.Enchant<HighQuality>(enchantedCard, 1m);
                reward.ModifyCard(enchantedCard, this);
                modified = true;
            }
        }

        return modified;
    }
}