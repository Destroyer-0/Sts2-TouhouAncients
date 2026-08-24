using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using TouhouAncients.Scripts.Enchantment;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 吞天之勺：向牌组中加入卡牌时，吞噬之（不加入牌组）并获得4最大生命。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class SkySwallowingSpoon : TouhouAncientRelics
{
    public override string DefaultFileName => "yuuma_default";

    [SavedProperty]
    public int TouhouAncients_SwallowedCards
    {
        get => swallowedCards;
        set
        {
            AssertMutable();
            swallowedCards = value;
            InvokeDisplayAmountChanged();
        }
    }

    private int swallowedCards;
    public override bool ShowCounter => true;
    public override int DisplayAmount => TouhouAncients_SwallowedCards;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new MaxHpVar(4), new StringVar("EnchantmentName", ModelDb.Enchantment<BloodPond>().Title.GetFormattedText())];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromEnchantment<BloodPond>();
    //
    // public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    // {
    //     if (base.Owner.Creature.IsDead || card.Owner != base.Owner)
    //     {
    //         return;
    //     }
    //     CardPile? pile = card.Pile;
    //     if (pile != null && pile.Type == PileType.Deck && card.Enchantment == null)
    //     {
    //         Flash();
    //         CardCmd.Enchant<BloodPond>(card, 1m);
    //     }
    // }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        var list = Owner.Deck.Cards
            .Where(c => c.Enchantment is BloodPond)
            .ToList();
        if (list.Count > 0)
        {
            Flash();
            foreach (var cardModel in list)
            {
                var time = (cardModel.Type == CardType.Curse ? 3 : 1);
                TouhouAncients_SwallowedCards += time;
                await CardPileCmd.RemoveFromDeck(cardModel);
                await CreatureCmd.GainMaxHp(Owner.Creature, time * base.DynamicVars["MaxHp"].IntValue);
            }

            Grow();
        }

        return;
    }

    private void Grow()
    {
        NCombatRoom.Instance?.GetCreatureNode(base.Owner.Creature)
            ?.ScaleTo(MathF.Min(2.5f, 1 + TouhouAncients_SwallowedCards * 0.04f), 0f);
    }


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
        if (card.Enchantment != null)
        {
            card.ClearEnchantmentInternal();
        }

        var bloodPond = ModelDb.Enchantment<BloodPond>();
        if (!bloodPond.CanEnchant(card)) return false;
        newCard = EnchantCard(card);
        return true;
    }

    private void EnchantValidCards(List<CardCreationResult> options)
    {
        var bloodPond = ModelDb.Enchantment<BloodPond>();
        foreach (var option in options)
        {
            var card = option.Card;
            if (card.Enchantment != null)
            {
                card.ClearEnchantmentInternal();
            }

            if (!bloodPond.CanEnchant(card)) continue;
            option.ModifyCard(EnchantCard(card), this);
        }
    }

    private CardModel EnchantCard(CardModel card)
    {
        var enchanted = base.Owner.RunState.CloneCard(card);
        CardCmd.Enchant<BloodPond>(enchanted, 1m);
        return enchanted;
    }
    public override Task AfterRoomEntered(AbstractRoom _)
    {
        Grow();
        return Task.CompletedTask;
    }
}