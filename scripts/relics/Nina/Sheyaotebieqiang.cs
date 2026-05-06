using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using TouhouAncients.Scripts.Enchantment;
using TouhouAncients.scripts.Keywords;

namespace TouhouAncients.Scripts.relics;

[Pool(typeof(SharedRelicPool))]
public class Sheyaotebieqiang : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new StringVar("EnchantmentName", ModelDb.Enchantment<SnakeBiteUpgrade>().Title.GetFormattedText())];
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<Snakebite>()
            .Concat(HoverTipFactory.FromEnchantment<SnakeBiteUpgrade>());

    public override async Task AfterObtained()
    {
        // 将蛇咬加入牌组
        List<CardPileAddResult> results = new List<CardPileAddResult>();
        var snakebite = base.Owner.RunState.CreateCard(ModelDb.Card<Snakebite>(), base.Owner);

        CardCmd.Enchant<SnakeBiteUpgrade>(snakebite, 1m);
        results.Add(await CardPileCmd.Add(snakebite, PileType.Deck));
        CardCmd.PreviewCardPileAdd(results, 2f);
        
        var existing = Owner.Deck.Cards
            .Where(c => c.Tags.Contains(TouhouAncientCardTags.TouhouAncientSnakeBite))
            .Where(c => c.Enchantment == null)
            .ToList();

        foreach (var card in existing)
        {
            CardCmd.Enchant<SnakeBiteUpgrade>(card, 1m);
            GD.PrintErr($"蛇毒：附魔成功");
        }
    }

    /// <summary>
    /// 卡牌奖励中带有蛇咬标签的卡牌被附魔：蛇毒。
    /// </summary>
    public override bool TryModifyCardRewardOptionsLate(Player player, List<CardCreationResult> cardRewards, CardCreationOptions options)
    {
        if (player != base.Owner) return false;

        bool modified = false;
        foreach (var reward in cardRewards)
        {
            var card = reward.Card;
            if (!card.Tags.Contains(TouhouAncientCardTags.TouhouAncientSnakeBite)) continue;
            if (card.Enchantment != null) continue;

            var enchantedCard = base.Owner.RunState.CloneCard(card);
            CardCmd.Enchant<SnakeBiteUpgrade>(enchantedCard, 1m);
            reward.ModifyCard(enchantedCard, this);
            modified = true;
        }

        return modified;
    }
}