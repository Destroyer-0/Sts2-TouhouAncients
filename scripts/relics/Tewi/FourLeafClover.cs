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
[Pool(typeof(EventRelicPool))]
public class FourLeafClover : TouhouAncientRelics
{
    private int rewardsSinceLastRare;

    [SavedProperty]
    public int TouhouAncients_RewardsSinceLastRare
    {
        get => rewardsSinceLastRare;
        set
        {
            AssertMutable();
            rewardsSinceLastRare = value;
            InvokeDisplayAmountChanged();
        }
    }

    public override bool ShowCounter => true;
    public override int DisplayAmount => TouhouAncients_RewardsSinceLastRare;

    public override bool TryModifyCardRewardOptions(Player player, List<CardCreationResult> options,
        CardCreationOptions creationOptions)
    {
        if (base.Owner != player) return false;
        if (creationOptions.Source != CardCreationSource.Encounter) return false;

        TouhouAncients_RewardsSinceLastRare++;

        if (TouhouAncients_RewardsSinceLastRare % 2 == 0)
        {
            Flash();

            var enumerable = (from c in creationOptions.GetPossibleCards(player)
                where c.Rarity == CardRarity.Rare &&
                      options.TrueForAll((CardCreationResult o) => o.originalCard.Id != c.Id)
                select c).ToList();
            if (!enumerable.Any())
            {
                enumerable = (from c in creationOptions.GetPossibleCards(player)
                    where c.Rarity == CardRarity.Rare
                    select c).ToList();
            }

            if (!enumerable.Any())
            {
                return false;
            }

            CardCreationOptions options2 =
                new CardCreationOptions(creationOptions.CardPools, CardCreationSource.Other, creationOptions.RarityOdds)
                    .WithFilter(x => enumerable.Contains(x))
                    .WithFlags(CardCreationFlags.NoModifyHooks | CardCreationFlags.NoRarityModification);
            CardModel cardModel = CardFactory.CreateForReward(base.Owner, 1, options2).FirstOrDefault()?.Card;
            if (TouhouAncients_RewardsSinceLastRare % 4 == 0)
            {
                if (cardModel.IsUpgradable)
                {
                    CardCmd.Upgrade(cardModel);
                }
                TouhouAncients_RewardsSinceLastRare = 0;
            }

            var result = new CardCreationResult(cardModel);
            result.ModifyCard(cardModel, this);
            options.Add(result);
            return true;
        }

        return false;
    }
}

//     public override bool TryModifyCardRewardOptionsLate(Player player, List<CardCreationResult> options,
//         CardCreationOptions creationOptions)
//     {
//         if (base.Owner != player) return false;
//         if (TouhouAncients_RewardsSinceLastRare % 4 == 0)
//         {
//             TouhouAncients_RewardsSinceLastRare = 0;
//             // 第二次稀有卡牌升级
//             var ourCards = options.Where(o => o.ModifyingRelics.Contains(this)).ToList();
//             foreach (var result in ourCards)
//             {
//                 var card = result.Card;
//                 if (card.IsUpgradable)
//                 {
//                     var cloned = base.Owner.RunState.CloneCard(card);
//                     CardCmd.Upgrade(cloned);
//                     result.ModifyCard(cloned, this);
//                 }
//             }
//
//             return ourCards.Count > 0;
//         }
//
//         return false;
//     }
// }