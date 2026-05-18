// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// using BaseLib.Utils;
// using MegaCrit.Sts2.Core.CardSelection;
// using MegaCrit.Sts2.Core.Commands;
// using MegaCrit.Sts2.Core.GameActions.Multiplayer;
// using MegaCrit.Sts2.Core.Localization;
// using MegaCrit.Sts2.Core.Entities.Cards;
// using MegaCrit.Sts2.Core.Entities.Players;
// using MegaCrit.Sts2.Core.Entities.Relics;
// using MegaCrit.Sts2.Core.HoverTips;
// using MegaCrit.Sts2.Core.Localization.DynamicVars;
// using MegaCrit.Sts2.Core.Models;
// using MegaCrit.Sts2.Core.Models.CardPools;
// using MegaCrit.Sts2.Core.Models.RelicPools;
// using MegaCrit.Sts2.Core.Rewards;
// using MegaCrit.Sts2.Core.Rooms;
//
// namespace TouhouAncients.Scripts.relics;
//
// /// <summary>
// /// 至尊天印：拾起时选择一张升级后的先古之民卡牌加入牌组。不再掉落卡牌奖励。
// /// </summary>
// [Pool(typeof(SharedRelicPool))]
// public class SupremeHeavenSeal : TouhouAncientRelics
// {
//     public override bool HasUponPickupEffect => true;
//
//     public override async Task AfterObtained()
//     {
//         var player = base.Owner;
//
//         // 从EventCardPool中获取所有先古之民卡牌，让玩家选择一张并升级
//         var eventPool = ModelDb.CardPool<EventCardPool>();
//         var ancientCards = eventPool.AllCards
//             .Where(c => c.Rarity == CardRarity.Ancient)
//             .ToList();
//
//         if (ancientCards.Count == 0) return;
//
//         // 使用 CardSelector 让玩家选择
//         var prefs = new CardSelectorPrefs(
//             new LocString("TouhouAncients", "SUPREME_HEAVEN_SEAL_SELECT"),
//             1);
//         var selected = (await CardSelectCmd.FromSimpleGrid(
//             new ThrowingPlayerChoiceContext(),
//             ancientCards,
//             player,
//             prefs
//         )).FirstOrDefault();
//
//         if (selected != null)
//         {
//             // 升级并加入牌组
//             // 创建卡牌 -> 升级 -> 加入牌组
//             var newCard = player.RunState.CreateCard(selected, player);
//             newCard.UpgradeInternal();
//             var addResult = await CardPileCmd.Add(newCard, PileType.Deck);
//             CardCmd.PreviewCardPileAdd(addResult);
//         }
//     }
//
//     /// <summary>
//     /// 不再掉落卡牌奖励——移除所有 CardReward
//     /// </summary>
//     public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
//     {
//         if (player != base.Owner) return false;
//         var modified = false;
//         for (int i = rewards.Count - 1; i >= 0; i--)
//         {
//             if (rewards[i] is CardReward)
//             {
//                 rewards.RemoveAt(i);
//                 modified = true;
//             }
//         }
//         return modified;
//     }
// }
