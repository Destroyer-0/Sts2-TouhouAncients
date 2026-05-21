using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.Rewards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 领主防晒霜：卡牌奖励掉落的牌一定为稀有牌，且你可以选择全都要。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class LordsSunscreenCream : TouhouAncientRelics
{
    private const string _takeAllKey = "TAKE_ALL";

    /// <summary>
    /// 使卡牌奖励中出现的卡牌全部为稀有牌。
    /// </summary>
    public override CardCreationOptions ModifyCardRewardCreationOptions(Player player, CardCreationOptions options)
    {
        if (base.Owner != player) return options;

        // 只修改战斗遭遇的卡牌奖励
        if (options.Source != CardCreationSource.Encounter) return options;
        if (options.Flags.HasFlag(CardCreationFlags.NoRarityModification)) return options;

        // 用自定义池过滤出稀有牌
        var rareCards = options.GetPossibleCards(player)
            .Where(c => c.Rarity == CardRarity.Rare)
            .ToList();

        if (rareCards.Count == 0) return options;

        return options.WithCustomPool(rareCards, CardRarityOddsType.Uniform);
    }

    /// <summary>
    /// 添加"我全都要"选项，将卡牌奖励中的所有牌加入牌组。
    /// </summary>
    public override bool TryModifyCardRewardAlternatives(Player player, CardReward cardReward, List<CardRewardAlternative> alternatives)
    {
        if (base.Owner != player) return false;

        alternatives.Add(new CardRewardAlternative(
            _takeAllKey,
            () => OnTakeAll(player, cardReward),
            PostAlternateCardRewardAction.DismissScreenAndRemoveReward));
        return true;
    }

    private async Task OnTakeAll(Player player, CardReward cardReward)
    {
        Flash();

        var addResults = new List<CardPileAddResult>();
        foreach (var card in cardReward.Cards.ToList())
        {
            addResults.Add(await CardPileCmd.Add(card, PileType.Deck));
        }
        CardCmd.PreviewCardPileAdd(addResults, 2f);
    }
}
