using System.Collections.Generic;
using System.Linq;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 四十叶草：每2次卡牌奖励额外出现一张稀有卡牌，第二次以此法出现的稀有卡牌升级。
/// 参照 LastingCandy 实现。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class FourLeafClover : TouhouAncientRelics
{
    [SavedProperty]
    public int RewardsSinceLastRare { get; set; }

    public override bool TryModifyCardRewardOptions(Player player, List<CardCreationResult> options, CardCreationOptions creationOptions)
    {
        if (base.Owner != player) return false;
        if (creationOptions.Source != CardCreationSource.Encounter) return false;

        RewardsSinceLastRare++;

        if (RewardsSinceLastRare >= 2)
        {
            RewardsSinceLastRare = 0;

            // 从当前选项中获取一张稀有卡牌
            var rareCards = creationOptions.GetPossibleCards(player)
                .Where(c => c.Rarity == CardRarity.Rare)
                .ToList();
            if (!rareCards.Any()) return false;

            var rareOptions = new CardCreationOptions(rareCards, CardCreationSource.Other, CardRarityOddsType.Uniform)
                .WithFlags(CardCreationFlags.NoModifyHooks | CardCreationFlags.NoCardPoolModifications);

            var created = CardFactory.CreateForReward(base.Owner, 1, rareOptions).FirstOrDefault();
            if (created?.Card == null) return false;

            Flash();
            var result = new CardCreationResult(created.Card);
            result.ModifyCard(created.Card, this);
            options.Add(result);
        }

        return true;
    }

    public override bool TryModifyCardRewardOptionsLate(Player player, List<CardCreationResult> options, CardCreationOptions creationOptions)
    {
        if (base.Owner != player) return false;

        // 第二次稀有卡牌升级
        var ourCards = options.Where(o => o.ModifyingRelics.Contains(this)).ToList();
        foreach (var result in ourCards)
        {
            var card = result.Card;
            if (card.IsUpgradable)
            {
                var cloned = base.Owner.RunState.CloneCard(card);
                CardCmd.Upgrade(cloned);
                result.ModifyCard(cloned, this);
            }
        }

        return ourCards.Count > 0;
    }
}
