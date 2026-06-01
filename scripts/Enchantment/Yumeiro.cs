using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;

namespace TouhouAncients.Scripts.Enchantment;

/// <summary>
/// 梦色：费用-1，打出后抽一张牌，失去梦色附魔并变化为其他附魔。
/// </summary>
public class Yumeiro : CustomEnchantmentModel
{
    public override bool HasExtraCardText => false;

    // public override bool CanEnchant(CardModel card)
    // {
    //     return !card.HasStarCostX && !card.EnergyCost.CostsX && base.CanEnchant(card);
    // }

    protected override void OnEnchant()
    {
        if (!HasCard) return;
        if (Card.EnergyCost.CostsX) return;
        
        base.Card.EnergyCost.AddUntilPlayed(-1);
        //Card.EnergyCost.SetUntilPlayed(-1);
    }

    private static List<EnchantmentModel>? s_allEnchantments;

    private static List<EnchantmentModel> AllEnchantments
    {
        get
        {
            if (s_allEnchantments == null)
            {
                s_allEnchantments = new List<EnchantmentModel>();
                var enchantmentTypes = ModelDb.AllAbstractModelSubtypes
                    .Where(t => t != null && t.IsSubclassOf(typeof(EnchantmentModel)) && !t.IsAbstract);

                foreach (Type type in enchantmentTypes)
                {
                    try
                    {
                        ModelId id = ModelDb.GetId(type);
                        EnchantmentModel? enchantment = ModelDb.GetByIdOrNull<EnchantmentModel>(id);

                        if (enchantment != null)
                        {
                            s_allEnchantments.Add(enchantment);
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }

            return s_allEnchantments;
        }
    }

    public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay?.Card != base.Card) return;
        if (Card.HasBeenRemovedFromState) return;

        var player = base.Card.Owner;
        var card = Card;
        var cardType = card.Type;

        //Card.EnergyCost.ResetForDowngrade();
        CardCmd.ClearEnchantment(Card);
        // 失去梦色附魔并变化为其他附魔
        var availableEnchantments = AllEnchantments
            .Where(e =>
            {
                if (e is Bloodshed) return false;
                if (e is DeprecatedEnchantment) return false;
                if (e is Yumeiro) return false;
                if (e is Goopy) return false;
                if (e is TouhouAncientEnchantmentModel { CanBeRandomSelected: false }) return false;
                if (e is Inky && cardType != CardType.Attack) return false;
                return e.CanEnchant(card);
            })
            .ToList();

        if (availableEnchantments.Count > 0)
        {
            var randomEnchantment =
                availableEnchantments.UnstableShuffle(player.RunState.Rng.CombatCardGeneration).First();
            // 附加新的随机附魔
            CardCmd.Enchant(randomEnchantment.ToMutable(), card, 3m);
        }
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        if (cardPlay?.Card != base.Card) return;
        if (base.Status != EnchantmentStatus.Normal) return;

        var player = base.Card.Owner;

        // 抽一张牌
        await CardPileCmd.Draw(choiceContext, 1, player);
    }
}