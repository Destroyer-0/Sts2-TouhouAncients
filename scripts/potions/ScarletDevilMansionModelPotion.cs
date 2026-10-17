// using System.Threading.Tasks;
// using BaseLib.Abstracts;
// using BaseLib.Utils;
// using MegaCrit.Sts2.Core.Commands;
// using MegaCrit.Sts2.Core.Entities.Cards;
// using MegaCrit.Sts2.Core.Entities.Creatures;
// using MegaCrit.Sts2.Core.Entities.Potions;
// using MegaCrit.Sts2.Core.GameActions.Multiplayer;
// using MegaCrit.Sts2.Core.Models;
// using MegaCrit.Sts2.Core.Models.Cards;
// using MegaCrit.Sts2.Core.Models.PotionPools;
//
// namespace TouhouAncients.Scripts.potions;
//
// /// <summary>
// /// 红魔馆模型：按固定序列用手牌填满大爆炸与碎屑。
// /// </summary>
// [Pool(typeof(SharedPotionPool))]
// public sealed class ScarletDevilMansionModelPotion : CustomPotionModel
// {
//     public override PotionRarity Rarity => PotionRarity.Rare;
//
//     public override PotionUsage Usage => PotionUsage.CombatOnly;
//
//     public override TargetType TargetType => TargetType.Self;
//     public override string? CustomPackedImagePath => $"res://images/potion/{GetType().Name}.png";
//     public override string? CustomPackedOutlinePath => $"res://images/potion/{GetType().Name}_outline.png";
//
//     protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
//     {
//         var player = base.Owner;
//         var combatState = player.Creature.CombatState;
//         if (combatState == null) return;
//
//         int missing = CardPile.MaxCardsInHand - PileType.Hand.GetPile(player).Cards.Count;
//         for (int i = 0; i < missing; i++)
//         {
//             // 序列 大爆炸-碎屑-大爆炸-大爆炸-碎屑-大爆炸
//             CardModel card = i % 3 is 1 
//                 ? combatState.CreateCard<Debris>(player)
//                 : combatState.CreateCard<BigBang>(player);
//             await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, creator: player);
//         }
//     }
// }
